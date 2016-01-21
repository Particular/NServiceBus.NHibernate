namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using Config;
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
    using NUnit.Framework;

    abstract class InMemoryDBFixture
    {
        protected TimeoutPersister persister;
        protected ISessionFactory sessionFactory;

        string connectionString = $@"Data Source={Path.GetTempFileName()};Version=3;New=True;";
        const string dialect = "NHibernate.Dialect.SQLiteDialect";

        [SetUp]
        public void Setup()
        {
            var configuration = new global::NHibernate.Cfg.Configuration()
              .AddProperties(new Dictionary<string, string>
                {
                    { "dialect", dialect },
                    { global::NHibernate.Cfg.Environment.ConnectionString, connectionString }
                });
            var mapper = new ModelMapper();
            mapper.AddMapping<TimeoutEntityMap>();

            configuration.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            new SchemaExport(configuration).Create(false, true);

            sessionFactory = configuration.BuildSessionFactory();

            persister = new TimeoutPersister("MyTestEndpoint", sessionFactory);
        }
    }
}
