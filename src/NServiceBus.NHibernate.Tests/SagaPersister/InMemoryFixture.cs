namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using AutoPersistence;
    using global::NHibernate;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    class InMemoryFixture<T> where T : Saga
    {
        protected SagaPersister SagaPersister;
        protected ISessionFactory SessionFactory;
        protected string ConnectionString;

        [SetUp]
        public void SetUp()
        {
            ConnectionString = $@"Data Source={Path.GetTempFileName()};New=True;";

            var configuration = new global::NHibernate.Cfg.Configuration()
                .AddProperties(new Dictionary<string, string>
                {
                    { "dialect", dialect },
                    { global::NHibernate.Cfg.Environment.ConnectionString, ConnectionString }
                });

            var metaModel = new SagaMetadataCollection();
            metaModel.Initialize(new[] { typeof(T) });
            var metadata = metaModel.Find(typeof(T));

            var modelMapper = new SagaModelMapper(metaModel, new[] { metadata.SagaEntityType });

            configuration.AddMapping(modelMapper.Compile());

            SessionFactory = configuration.BuildSessionFactory();

            new OptimizedSchemaUpdate(configuration).Execute(false, true);

            SagaPersister = new SagaPersister();
        }

        [TearDown]
        public void Cleanup()
        {
            SessionFactory.Close();
        }

        const string dialect = "NHibernate.Dialect.SQLiteDialect";
    }
}