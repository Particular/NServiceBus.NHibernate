namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using global::NHibernate;
    using global::NHibernate.AdoNet.Util;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Tool.hbm2ddl;
    using global::NHibernate.Util;
    using Environment = global::NHibernate.Cfg.Environment;

    class OptimizedSchemaUpdate
    {
        public OptimizedSchemaUpdate(global::NHibernate.Cfg.Configuration cfg) 
            : this(cfg, cfg.Properties)
        {
        }

        OptimizedSchemaUpdate(global::NHibernate.Cfg.Configuration cfg, IDictionary<string, string> configProperties)
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
        ///     Execute the schema updates
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
                    typeof(MsSql2012Dialect).FullName,
                };

                foreach (var item in updateSQL)
                {
                    var sql = item;

                    if (dialectScopes.Contains(dialect.GetType().FullName))
                    {
                        if (sql.StartsWith("create table TimeoutEntity"))
                        {
                            sql = sql.Replace("primary key (Id)", "primary key nonclustered (Id)");
                        }
                        sql = sql.Replace("create index TimeoutEntity_EndpointIdx on TimeoutEntity (Time, Endpoint)", "create clustered index TimeoutEntity_EndpointIdx on TimeoutEntity (Endpoint, Time)");
                    }

                    var formatted = formatter.Format(sql);

                    try
                    {
                        scriptAction?.Invoke(formatted);
                        if (doUpdate)
                        {
                            log.Debug(sql);
                            stmt.CommandText = sql;
                            stmt.ExecuteNonQuery();
                        }
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                        log.Error("Unsuccessful: " + sql, e);
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

        static IInternalLogger log = LoggerProvider.LoggerFor(typeof(OptimizedSchemaUpdate));
        global::NHibernate.Cfg.Configuration configuration;
        IConnectionHelper connectionHelper;
        Dialect dialect;
        List<Exception> exceptions;
        IFormatter formatter;
    }
}