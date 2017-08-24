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

    class IncorrectIndexDetector
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(IncorrectIndexDetector));

        public IncorrectIndexDetector(Configuration configuration)
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

        public void LogWarningIfTimeoutEntityIndexIsIncorrect()
        {
            try
            {
                connectionHelper.Prepare();
                var connection = connectionHelper.Connection;

                var index = GetIndex(connection, timeoutEntityMapping, TimeoutEntityMap.EndpointIndexName);
                if (index == null)
                {
                    Logger.Warn($"Could not find {TimeoutEntityMap.EndpointIndexName} index. This may cause significant performance degradation of message deferral. Consult NServiceBus NHibernate persistence documentation for details on how to create this index.");
                    return;
                }

                var indexCorrect = index.ColumnsCount == 2
                    && index.ColumnNameAt(1).Equals(nameof(TimeoutEntity.Endpoint), StringComparison.InvariantCultureIgnoreCase)
                    && index.ColumnNameAt(2).Equals(nameof(TimeoutEntity.Time), StringComparison.InvariantCultureIgnoreCase);

                Logger.Debug($"Detected {TimeoutEntityMap.EndpointIndexName} ({index.ColumnNameAt(1)}, {index.ColumnNameAt(2)})");

                if (!indexCorrect)
                {
                    Logger.Warn($"The {TimeoutEntityMap.EndpointIndexName} index has incorrect column order. This may cause significant performance degradation of message deferral. Consult NServiceBus NHibernate persistence documentation for details on how to create this index.");
                }
            }
            catch (Exception e)
            {
                Logger.Warn($"Could not inspect {TimeoutEntityMap.EndpointIndexName} index definition.", e);
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

        private IIndex GetIndex(DbConnection connection, PersistentClass entity, string indexName)
        {


            // Check if running on SQL Server.
            if (typeof(MsSql2005Dialect).IsAssignableFrom(dialect.GetType()))
            {
                var restrictions = new string[5]
                {
                    entity.Table.Catalog,
                    entity.Table.Schema,
                    entity.Table.Name,
                    indexName,
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
                var columnName = from row in indexColumns.AsEnumerable()
                                 where position == (int)row["ordinal_position"]
                                 select row["column_name"].ToString();

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