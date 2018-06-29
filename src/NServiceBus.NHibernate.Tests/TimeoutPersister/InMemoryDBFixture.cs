namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Config;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
    using Persistence.NHibernate;
    using NUnit.Framework;

    abstract class InMemoryDBFixture
    {
#if USE_SQLSERVER
        private readonly string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True;";
        private const string dialect = "NHibernate.Dialect.MsSql2012Dialect";
#else
#endif

        protected TimeoutPersister persister;
        protected ISessionFactory sessionFactory;
        SchemaExport schema;

        [SetUp]
        public async Task Setup()
        {
            var cfg = new Configuration()
                .DataBaseIntegration(x =>
                {
                    x.Dialect<SQLiteDialect>();
                    x.ConnectionString = $"Data Source={Path.GetTempFileName()};Version=3;New=True;";
                });

            var mapper = new ModelMapper();
            mapper.AddMapping<TimeoutEntityMap>();

            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            schema = new SchemaExport(cfg);
            await schema.CreateAsync(false, true);

            sessionFactory = cfg.BuildSessionFactory();

            persister = new TimeoutPersister("MyTestEndpoint", sessionFactory, new NHibernateSynchronizedStorageAdapter(sessionFactory), new NHibernateSynchronizedStorage(sessionFactory), TimeSpan.FromMinutes(2));
        }

        [TearDown]
        public Task TearDown() => schema.DropAsync(false, true);
    }
}
