namespace NServiceBus.NHibernate.Tests.SynchronizedStorage
{
    using System.Threading.Tasks;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Driver;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
    using NServiceBus.Outbox.NHibernate;
    using NUnit.Framework;

    abstract class InMemoryDBFixture
    {
        protected ISessionFactory SessionFactory;
        SchemaExport schema;

        [SetUp]
        public async Task SetupContext()
        {
            var cfg = new Configuration()
                .DataBaseIntegration(x =>
                {
                    x.Dialect<MsSql2012Dialect>();
                    x.Driver<MicrosoftDataSqlClientDriver>();
                    x.ConnectionString = Consts.SqlConnectionString;
                });

            var mapper = new ModelMapper();
            mapper.AddMapping<TestEntity.Mapping>();
            mapper.AddMapping<OutboxRecordMapping>();
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            schema = new SchemaExport(cfg);
            await schema.CreateAsync(false, true);
            SessionFactory = cfg.BuildSessionFactory();
        }

        [TearDown]
        public async Task TearDown()
        {
            await schema.DropAsync(false, true);
            SessionFactory.Dispose();
        }
    }
}