namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config.Internal;
    using global::NHibernate.Cfg;
    using global::NHibernate.Impl;
    using Saga;

    public static class SessionFactoryHelper
    {
        public static SessionFactoryImpl Build()
        {
            var types = Types();

            var builder = new SessionFactoryBuilder(types);
            var properties = SQLiteConfiguration.InMemory();

            return builder.Build(new Configuration().AddProperties(properties)) as SessionFactoryImpl;
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