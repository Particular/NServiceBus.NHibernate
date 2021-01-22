namespace NServiceBus.TimeoutPersisters.NHibernate.Installer
{
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Mapping;
    using global::NHibernate.Tool.hbm2ddl;
    using Config;
    using Logging;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;

    class TimeoutsIndexValidator
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(TimeoutsIndexValidator));

        public TimeoutsIndexValidator(Configuration configuration)
        {
            dialect = Dialect.GetDialect(configuration.Properties);
            connectionHelper = new ManagedProviderConnectionHelper(MergeProperties(configuration.Properties));
            timeoutEntityMapping = configuration.GetClassMapping(typeof(TimeoutEntity));
        }

        Dictionary<string, string> MergeProperties(IDictionary<string, string> properties)
        {
            var props = new Dictionary<string, string>(dialect.DefaultProperties);
            foreach (var prop in properties)
            {
                props[prop.Key] = prop.Value;
            }
            return props;
        }

        public IndexValidationResult Validate()
        {
            try
            {
                connectionHelper.Prepare();
                var connection = connectionHelper.Connection;

                var index = GetIndex(connection, timeoutEntityMapping, TimeoutEntityMap.EndpointIndexName);
                if (index == null)
                {
                    return new IndexValidationResult
                    {
                        IsValid = false,
                        ErrorDescription = $"Could not find {TimeoutEntityMap.EndpointIndexName} index. This may cause significant performance degradation of message deferral. Consult NServiceBus NHibernate persistence documentation for details on how to create this index."
                    };
                }

                Logger.Debug($"Detected {TimeoutEntityMap.EndpointIndexName} ({index.ColumnNameAt(1)}, {index.ColumnNameAt(2)})");

                var validColumns = index.ColumnsCount == 2
                    && index.ColumnNameAt(1).Equals(nameof(TimeoutEntity.Endpoint), StringComparison.InvariantCultureIgnoreCase)
                    && index.ColumnNameAt(2).Equals(nameof(TimeoutEntity.Time), StringComparison.InvariantCultureIgnoreCase);

                if (!validColumns)
                {
                    return new IndexValidationResult
                    {
                        IsValid = false,
                        ErrorDescription = $"The {TimeoutEntityMap.EndpointIndexName} index has incorrect column order. This may cause significant performance degradation of message deferral. Consult NServiceBus NHibernate persistence documentation for details on how to create this index."
                    };
                }

                return new IndexValidationResult { IsValid = true };
            }
            catch (Exception e)
            {
                return new IndexValidationResult
                {
                    IsValid = false,
                    ErrorDescription = $"Could not inspect {TimeoutEntityMap.EndpointIndexName} index definition.",
                    Exception = e
                };
            }
            finally
            {
                try
                {
                    connectionHelper.Release();
                }
                catch (Exception e)
                {
                    Logger.Error("Error closing connection", e);
                }
            }
        }

        IIndex GetIndex(DbConnection connection, PersistentClass entity, string indexName)
        {
            // Check if running on SQL Server.
            if (typeof(MsSql2005Dialect).IsAssignableFrom(dialect.GetType()))
            {
                string EnsureUnquoted(string name)
                {
                    if (null == name || name.Length < 2)
                    {
                        return name;
                    }

                    if ('[' == name[0] && ']' == name[name.Length - 1]) // quoted?
                    {
                        name = name.Substring(1, name.Length - 2); // remove outer brackets
                        name = name.Replace("]]", "]"); // un-escape right-bracket
                    }

                    return name;
                }

                var restrictions = new[]
                {
                    EnsureUnquoted(entity.Table.Catalog),
                    EnsureUnquoted(entity.Table.Schema),
                    EnsureUnquoted(entity.Table.Name),
                    EnsureUnquoted(indexName),
                    null
                };
                return new SqlServerIndex(connection.GetSchema("IndexColumns", restrictions));
            }

            if (typeof(Oracle10gDialect).IsAssignableFrom(dialect.GetType()))
            {
                var restrictions = new string[5]
                {
                    entity.Table.Schema?.ToUpper(),
                    indexName?.ToUpper(),
                    null,
                    entity.Table.Name?.ToUpper(),
                    null
                };
                return new OracleIndex(connection.GetSchema("IndexColumns", restrictions));
            }

            return null;
        }

        public class IndexValidationResult
        {
            public bool IsValid { get; set; }

            public string ErrorDescription { get; set; }

            public Exception Exception { get; set; }
        }

        interface IIndex
        {
            int ColumnsCount { get; }
            string ColumnNameAt(int position);
        }

        class SqlServerIndex : IIndex
        {
            public SqlServerIndex(DataTable indexColumns)
            {
                this.indexColumns = indexColumns;
            }

            public int ColumnsCount => indexColumns.Rows.Count;

            public string ColumnNameAt(int position)
            {
                var columnName = indexColumns.AsEnumerable()
                    .Where(row => position == (int)row["ordinal_position"])
                    .Select(row => row["column_name"].ToString());

                return columnName.FirstOrDefault();
            }

            readonly DataTable indexColumns;
        }

        class OracleIndex : IIndex
        {
            public OracleIndex(DataTable indexColumns)
            {
                this.indexColumns = indexColumns;
            }

            public int ColumnsCount => indexColumns.Rows.Count;

            public string ColumnNameAt(int position)
            {
                var columnName = from row in indexColumns.AsEnumerable()
                                 where position == (decimal)row["COLUMN_POSITION"]
                                 select row["COLUMN_NAME"].ToString();

                return columnName.FirstOrDefault();
            }

            readonly DataTable indexColumns;
        }

        readonly IConnectionHelper connectionHelper;
        readonly Dialect dialect;
        readonly PersistentClass timeoutEntityMapping;
    }
}
