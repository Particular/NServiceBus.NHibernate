namespace NServiceBus.SagaPersisters.NHibernate.Tests;

using System;
using System.Linq;
using AutoPersistence;
using global::NHibernate.Cfg;
using global::NHibernate.Dialect;
using global::NHibernate.Driver;
using global::NHibernate.Impl;
using NServiceBus.NHibernate.Tests;
using Sagas;

public static class SessionFactoryHelper
{
    public static SessionFactoryImpl Build(Type[] types)
    {
        var cfg = new Configuration()
            .DataBaseIntegration(x =>
            {
                x.Dialect<MsSql2012Dialect>();
                x.Driver<MicrosoftDataSqlClientDriver>();
                x.ConnectionString = Consts.SqlConnectionString;
            });

        var metaModel = new SagaMetadataCollection();

        metaModel.Initialize(types);

        var assemblies = types.Select(t => t.Assembly).Distinct();

        foreach (var assembly in assemblies)
        {
            cfg.AddAssembly(assembly);
        }

        SagaModelMapper.AddMappings(cfg, metaModel, types);

        return cfg.BuildSessionFactory() as SessionFactoryImpl;
    }
}