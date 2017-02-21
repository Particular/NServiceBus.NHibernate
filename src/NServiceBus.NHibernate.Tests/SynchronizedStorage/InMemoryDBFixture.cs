namespace NServiceBus.NHibernate.Tests.SynchronizedStorage
{
    using System.IO;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
    using NUnit.Framework;

    class InMemoryDBFixture
    {
        protected ISessionFactory SessionFactory;

        [SetUp]
        public void SetupContext()
        {
            var cfg = new Configuration()
                .DataBaseIntegration(x =>
                {
                    x.Dialect<SQLiteDialect>();
                    x.ConnectionString = $@"Data Source={Path.GetTempFileName()};Version=3;New=True;";
                });

            var mapper = new ModelMapper();
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            new SchemaExport(cfg).Create(false, true);
            SessionFactory = cfg.BuildSessionFactory();
        }
    }
}