namespace NServiceBus.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using global::NHibernate.Tool.hbm2ddl;
    using NServiceBus.SagaPersisters.NHibernate;
    using NServiceBus.SagaPersisters.NHibernate.AutoPersistence;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Environment = global::NHibernate.Cfg.Environment;

    abstract class InMemoryFixture
    {
        protected abstract Type[] SagaTypes { get; }

        [SetUp]
        public void SetUp()
        {
            ConnectionString = $@"Data Source={Path.GetTempFileName()};New=True;";

            var configuration = new Configuration()
                .AddProperties(new Dictionary<string, string>
                {
                    {"dialect", dialect},
                    {Environment.ConnectionString, ConnectionString}
                });

            var metaModel = new SagaMetadataCollection();

            metaModel.Initialize(SagaTypes);

            var sagaDataTypes = new List<Type>();
            using (var enumerator = metaModel.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    sagaDataTypes.Add(enumerator.Current.SagaEntityType);
                }
            }

            sagaDataTypes.Add(typeof(ContainSagaData));

            SagaModelMapper.AddMappings(configuration, metaModel, sagaDataTypes);
            SessionFactory = configuration.BuildSessionFactory();

            new SchemaUpdate(configuration).Execute(true, true);

            SagaPersister = new SagaPersister();
        }

        [TearDown]
        public void Cleanup()
        {
            SessionFactory.Close();
        }

        string ConnectionString;

        protected SagaPersister SagaPersister;
        protected ISessionFactory SessionFactory;

        const string dialect = "NHibernate.Dialect.SQLiteDialect";
    }
}