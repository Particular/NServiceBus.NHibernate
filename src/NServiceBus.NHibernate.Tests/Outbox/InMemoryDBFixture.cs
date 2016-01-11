//#define USE_SQLSERVER

namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System.Collections.Generic;
    using System.IO;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.Persistence.NHibernate;
    using NUnit.Framework;
    using Environment = global::NHibernate.Cfg.Environment;

    abstract class InMemoryDBFixture
    {
        protected OutboxPersister persister;

#if USE_SQLSERVER
        private readonly string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True;";
        private const string dialect = "NHibernate.Dialect.MsSql2012Dialect";
#else
        readonly string connectionString = $@"Data Source={Path.GetTempFileName()};Version=3;New=True;";
        const string dialect = "NHibernate.Dialect.SQLiteDialect";
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

            new SchemaUpdate(configuration).Execute(false, true);

            SessionFactory = configuration.BuildSessionFactory();

            persister = new OutboxPersister(SessionFactory, "TestEndpoint");
        }

        [TearDown]
        public void TearDown()
        {
            SessionFactory.Close();
        }

        protected ISessionFactory SessionFactory;
    }
}