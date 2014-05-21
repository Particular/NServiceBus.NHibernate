namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Config;
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
    using NServiceBus.NHibernate.Internal;
    using NServiceBus.NHibernate.Tests.Outbox;
    using NUnit.Framework;

    abstract class InMemoryDBFixture
    {
        protected TimeoutStorage persister;
        protected ISessionFactory sessionFactory;

        private readonly string connectionString = String.Format(@"Data Source={0};Version=3;New=True;", Path.GetTempFileName());
        private const string dialect = "NHibernate.Dialect.SQLiteDialect";

        [SetUp]
        public void Setup()
        {
            var configuration = new global::NHibernate.Cfg.Configuration()
              .AddProperties(new Dictionary<string, string>
                {
                    { "dialect", dialect },
                    { global::NHibernate.Cfg.Environment.ConnectionString,connectionString }
                });
            var mapper = new ModelMapper();
            mapper.AddMapping<TimeoutEntityMap>();

            configuration.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            new SchemaExport(configuration).Create(false, true);

            sessionFactory = configuration.BuildSessionFactory();

            persister = new TimeoutStorage
            {
                SessionFactory = sessionFactory,
                DbConnectionProvider = new FakeDbConnectionProvider(sessionFactory.GetConnection()),
                EndpointName = "MyTestEndpoint"
            };
        }
    }
}
