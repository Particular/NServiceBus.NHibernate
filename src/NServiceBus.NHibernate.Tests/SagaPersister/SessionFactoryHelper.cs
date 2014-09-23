namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Features;
    using global::NHibernate.Cfg;
    using global::NHibernate.Impl;
    using Saga;
    using Settings;

    public static class SessionFactoryHelper
    {
        public static SessionFactoryImpl Build()
        {
            var types = Types();

            var builder = new NHibernateSagaStorage();
            var properties = SQLiteConfiguration.InMemory();

            var configuration = new Configuration().AddProperties(properties);
            var settings = new SettingsHolder();
            settings.Set("TypesToScan", types);
            builder.ApplyMappings(settings, configuration);
            return configuration.BuildSessionFactory() as SessionFactoryImpl;
        }

        public static List<Type> Types()
        {
            var assemblyContainingSagas = typeof(TestSaga).Assembly;
            var types = assemblyContainingSagas.GetTypes().ToList();
            types.Add(typeof(ContainSagaData));
            types.Remove(typeof(MyDerivedClassWithRowVersion));

            return types;
        }
    }
}