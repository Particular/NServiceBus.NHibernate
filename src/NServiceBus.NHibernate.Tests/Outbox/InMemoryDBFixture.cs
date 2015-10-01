//#define USE_SQLSERVER

namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System;
    using System.Collections.Generic;
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
    using NServiceBus.Outbox.NHibernate;
    using SagaPersisters.NHibernate.Tests;
    using System.IO;
    using NServiceBus.Outbox;
    using NUnit.Framework;

    abstract class InMemoryDBFixture
    {
        [SetUp]
        public void Setup()
        {
#if USE_SQLSERVER
            connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True;";
            dialect = "NHibernate.Dialect.MsSql2012Dialect";
#else
            connectionString = String.Format(@"Data Source={0};Version=3;New=True;", Path.GetTempFileName());
            dialect = "NHibernate.Dialect.SQLiteDialect";
#endif

            var mapper = new ModelMapper();
            mapper.AddMapping<OutboxEntityMap>();

            var configuration = new global::NHibernate.Cfg.Configuration()
                .AddProperties(new Dictionary<string, string>
                {
                    { "dialect", dialect },
                    { global::NHibernate.Cfg.Environment.ConnectionString,connectionString }
                });

            configuration.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            new SchemaUpdate(configuration).Execute(false, true);

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
            Console.WriteLine("TearDown");
            Session.Close();
            SessionFactory.Close();

            Session = null;
            SessionFactory = null;
            persister = null;
        }

        protected OutboxPersister persister;
        string connectionString;
        string dialect;
        protected ISession Session;
        protected ISessionFactory SessionFactory;
    }
}