namespace NServiceBus.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Tool.hbm2ddl;
    using SagaPersisters.NHibernate;
    using SagaPersisters.NHibernate.AutoPersistence;
    using Sagas;
    using NUnit.Framework;
    using Environment = global::NHibernate.Cfg.Environment;

    abstract class InMemoryFixture
    {
        protected abstract Type[] SagaTypes { get; }

        [SetUp]
        public async Task SetUp()
        {
            var connectionString = $"Data Source={Path.GetTempFileName()};New=True;";

            var configuration = new Configuration()
                .AddProperties(new Dictionary<string, string>
                {
                    {Environment.Dialect, typeof(SQLiteDialect).FullName},
                    {Environment.ConnectionString, connectionString}
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

            schema = new SchemaExport(configuration);
            await schema.CreateAsync(false, true);

            SagaPersister = new SagaPersister();
        }

        [TearDown]
        public async Task Cleanup()
        {
            await SessionFactory.CloseAsync();
            await schema.DropAsync(false, true);
        }

        protected SagaPersister SagaPersister;
        protected ISessionFactory SessionFactory;
        SchemaExport schema;
    }
}