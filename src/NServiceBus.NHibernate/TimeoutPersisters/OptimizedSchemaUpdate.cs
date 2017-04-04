namespace NServiceBus.TimeoutPersisters.NHibernate.Installer
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using global::NHibernate;
    using global::NHibernate.AdoNet.Util;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Mapping;
    using global::NHibernate.Tool.hbm2ddl;
    using global::NHibernate.Util;
    using NServiceBus.TimeoutPersisters.NHibernate.Config;
    using Environment = global::NHibernate.Cfg.Environment;

    class OptimizedSchemaUpdate
    {
        public OptimizedSchemaUpdate(Configuration cfg) : this(cfg, cfg.Properties)
        {
        }

        public OptimizedSchemaUpdate(Configuration cfg, IDictionary<string, string> configProperties)
        {
            configuration = cfg;
            dialect = Dialect.GetDialect(configProperties);
            var props = new Dictionary<string, string>(dialect.DefaultProperties);
            foreach (var prop in configProperties)
            {
                props[prop.Key] = prop.Value;
            }
            connectionHelper = new ManagedProviderConnectionHelper(props);
            exceptions = new List<Exception>();
            timeoutEntityMapping = configuration.GetClassMapping(typeof(TimeoutEntity));
            formatter = (PropertiesHelper.GetBoolean(Environment.FormatSql, configProperties, true) ? FormatStyle.Ddl : FormatStyle.None).Formatter;
        }

        // List of all Exceptions which occured during the export.
        public IList<Exception> Exceptions => exceptions;

        public void Execute(bool script, bool doUpdate)
        {
            if (script)
            {
                Execute(Console.WriteLine, doUpdate);
            }
            else
            {
                Execute(null, doUpdate);
            }
        }

        /// <summary>
        /// Execute the schema updates
        /// </summary>
        /// <param name="scriptAction">The action to write the each schema line.</param>
        /// <param name="doUpdate">Commit the script to DB</param>
        public void Execute(Action<string> scriptAction, bool doUpdate)
        {
            log.Info("Running hbm2ddl schema update");

            var autoKeyWordsImport = PropertiesHelper.GetString(Environment.Hbm2ddlKeyWords, configuration.Properties, "not-defined");
            autoKeyWordsImport = autoKeyWordsImport.ToLowerInvariant();
            if (autoKeyWordsImport == Hbm2DDLKeyWords.AutoQuote)
            {
                SchemaMetadataUpdater.QuoteTableAndColumns(configuration);
            }

            IDbCommand stmt = null;

            exceptions.Clear();

            try
            {
                DatabaseMetadata meta;
                try
                {
                    log.Info("fetching database metadata");
                    connectionHelper.Prepare();
                    var connection = connectionHelper.Connection;
                    meta = new DatabaseMetadata(connection, dialect);
                    stmt = connection.CreateCommand();
                }
                catch (Exception sqlException)
                {
                    exceptions.Add(sqlException);
                    log.Error("could not get database metadata", sqlException);
                    throw;
                }

                log.Info("updating schema");

                var updateSQL = configuration.GenerateSchemaUpdateScript(dialect, meta);

                var dialectScopes = new List<string>
                {
                    typeof(MsSql2005Dialect).FullName,
                    typeof(MsSql2008Dialect).FullName,
                    typeof(MsSql2012Dialect).FullName
                };

                var shouldOptimizeTimeoutEntity = timeoutEntityMapping != null;

                foreach (var item in updateSQL)
                {
                    var updateSqlStatement = item;

                    if (dialectScopes.Contains(dialect.GetType().FullName) && shouldOptimizeTimeoutEntity)
                    {
                        var qualifiedTimeoutEntityTableName = timeoutEntityMapping.Table.GetQualifiedName(dialect);

                        if (updateSqlStatement.StartsWith($"create table {qualifiedTimeoutEntityTableName}"))
                        {
                            updateSqlStatement = updateSqlStatement.Replace("primary key (Id)", "primary key nonclustered (Id)");
                        }
                        else if (updateSqlStatement.StartsWith($"create index {TimeoutEntityMap.EndpointIndexName}"))
                        {
                            updateSqlStatement = updateSqlStatement.Replace($"create index {TimeoutEntityMap.EndpointIndexName}", $"create clustered index {TimeoutEntityMap.EndpointIndexName}");
                        }
                    }

                    var formatted = formatter.Format(updateSqlStatement);

                    try
                    {
                        scriptAction?.Invoke(formatted);
                        if (doUpdate)
                        {
                            log.Debug(updateSqlStatement);
                            stmt.CommandText = updateSqlStatement;
                            stmt.ExecuteNonQuery();
                        }
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                        log.Error("Unsuccessful: " + updateSqlStatement, e);
                    }
                }

                log.Info("schema update complete");
            }
            catch (Exception e)
            {
                exceptions.Add(e);
                log.Error("could not complete schema update", e);
            }
            finally
            {
                try
                {
                    stmt?.Dispose();
                    connectionHelper.Release();
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                    log.Error("Error closing connection", e);
                }
            }
        }

        Configuration configuration;
        IConnectionHelper connectionHelper;
        Dialect dialect;
        List<Exception> exceptions;
        IFormatter formatter;
        PersistentClass timeoutEntityMapping;

        static IInternalLogger log = LoggerProvider.LoggerFor(typeof(OptimizedSchemaUpdate));
    }
}