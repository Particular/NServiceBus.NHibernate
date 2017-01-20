namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Config;
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
    using NServiceBus.Persistence.NHibernate;
    using NUnit.Framework;

    abstract class InMemoryDBFixture
    {
#if USE_SQLSERVER
        private readonly string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True;";
        private const string dialect = "NHibernate.Dialect.MsSql2012Dialect";
#else
        readonly string connectionString = $@"Data Source={Path.GetTempFileName()};Version=3;New=True;";
        const string dialect = "NHibernate.Dialect.SQLiteDialect";
#endif

        protected TimeoutPersister persister;
        protected ISessionFactory sessionFactory;

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

            persister = new TimeoutPersister("MyTestEndpoint", sessionFactory, new NHibernateSynchronizedStorageAdapter(sessionFactory), new NHibernateSynchronizedStorage(sessionFactory), TimeSpan.FromMinutes(2));
        }
    }
}
