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
#if !USE_SQLSERVER
    using System.IO;
#endif
    using NServiceBus.Outbox;
    using NUnit.Framework;


    abstract class InMemoryDBFixture
    {
        protected OutboxPersister persister;

#if USE_SQLSERVER
        private readonly string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True;";
        private const string dialect = "NHibernate.Dialect.MsSql2012Dialect";
#else
        private readonly string connectionString = String.Format(@"Data Source={0};Version=3;New=True;", Path.GetTempFileName());
        private const string dialect = "NHibernate.Dialect.SQLiteDialect";
#endif

        [SetUp]
        public void Setup()
        {
            var mapper = new ModelMapper();
            mapper.AddMapping<OutboxEntityMap>();
            mapper.AddMapping<TransportOperationEntityMap>();

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
                StorageSessionProvider = new FakeSessionProvider(SessionFactory, Session)
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
