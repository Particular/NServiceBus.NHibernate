namespace NServiceBus.TimeoutPersisters.NHibernate.Installer
{
    using System.Collections.Generic;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using Config;

    class SchemaFixUpHelper
    {
        static List<string> dialectScopes = new List<string>
        {
            typeof(MsSql2005Dialect).FullName,
            typeof(MsSql2008Dialect).FullName,
            typeof(MsSql2012Dialect).FullName
        };

        Configuration configuration;
        Dialect dialect;

        public SchemaFixUpHelper(Configuration configuration, Dialect dialect)
        {
            this.configuration = configuration;
            this.dialect = dialect;
        }

        public string FixUp(string item)
        {
            var timeoutEntityMapping = configuration.GetClassMapping(typeof(TimeoutEntity));
            var shouldOptimizeTimeoutEntity = timeoutEntityMapping != null;

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
            return updateSqlStatement;
        }
    }
}