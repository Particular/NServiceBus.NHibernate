namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using Features;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Impl;
    using NServiceBus.NHibernate.Tests;
    using Sagas;
    using Settings;

    public static class SessionFactoryHelper
    {
        public static SessionFactoryImpl Build(Type[] types)
        {
            var builder = new NHibernateSagaStorage();

            var cfg = new Configuration()
                .DataBaseIntegration(x =>
                {
                    x.Dialect<MsSql2012Dialect>();
                    x.ConnectionString = Consts.SqlConnectionString;
                });

            var settings = new SettingsHolder();

            var metaModel = new SagaMetadataCollection();
            metaModel.Initialize(types);
            settings.Set<SagaMetadataCollection>(metaModel);

            settings.Set("TypesToScan", types);
            builder.ApplyMappings(settings, cfg);
            return cfg.BuildSessionFactory() as SessionFactoryImpl;
        }
    }
}