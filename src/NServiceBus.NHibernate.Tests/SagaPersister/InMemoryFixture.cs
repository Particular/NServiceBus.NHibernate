namespace NServiceBus.NHibernate.Tests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using global::NHibernate;
using global::NHibernate.Cfg;
using global::NHibernate.Dialect;
using global::NHibernate.Driver;
using global::NHibernate.Tool.hbm2ddl;
using NUnit.Framework;
using SagaPersisters.NHibernate;
using SagaPersisters.NHibernate.AutoPersistence;
using Sagas;

abstract class InMemoryFixture
{
    protected abstract Type[] SagaTypes { get; }

    [SetUp]
    public async Task SetUp()
    {
        var cfg = new Configuration()
            .DataBaseIntegration(x =>
            {
                x.Dialect<MsSql2012Dialect>();
                x.Driver<MicrosoftDataSqlClientDriver>();
                x.ConnectionString = Consts.SqlConnectionString;
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

        SagaModelMapper.AddMappings(cfg, metaModel, sagaDataTypes);
        SessionFactory = cfg.BuildSessionFactory();

        schema = new SchemaExport(cfg);
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