//#define USE_SQLSERVER

namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System.Collections.Generic;
    using System.IO;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using global::NHibernate.Mapping.ByCode;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.SagaPersisters.NHibernate.Tests;
    using NServiceBus.TimeoutPersisters.NHibernate.Installer;
    using NUnit.Framework;

    abstract class InMemoryDBFixture
    {
        protected OutboxPersister persister;

#if USE_SQLSERVER
        private readonly string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True;";
        private const string dialect = "NHibernate.Dialect.MsSql2012Dialect";
#else
        private readonly string connectionString = $@"Data Source={Path.GetTempFileName()};Version=3;New=True;";
        private const string dialect = "NHibernate.Dialect.SQLiteDialect";
#endif

        [SetUp]
        public void Setup()
        {
            var mapper = new ModelMapper();
            mapper.AddMapping<OutboxEntityMap>();

            var configuration = new Configuration()
                .AddProperties(new Dictionary<string, string>
                {
                    {"dialect", dialect},
                    {Environment.ConnectionString, connectionString}
                });

            configuration.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            new OptimizedSchemaUpdate(configuration).Execute(false, true);

            SessionFactory = configuration.BuildSessionFactory();

            Session = SessionFactory.OpenSession();

            persister = new OutboxPersister
            {
                StorageSessionProvider = new FakeSessionProvider(SessionFactory, Session),
                EndpointName = "TestEndpoint"
            };
        }

        [TearDown]
        public void TearDown()
        {
            Session.Close();
            SessionFactory.Close();
        }

        protected ISession Session;
        protected ISessionFactory SessionFactory;
    }
}