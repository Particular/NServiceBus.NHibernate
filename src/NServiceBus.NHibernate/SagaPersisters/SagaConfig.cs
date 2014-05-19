namespace NServiceBus.NHibernate
{
    using System;
    using Persistence;

    public static class SagaConfig
    {
        public static void SagaTableNamingConvention(this PersistenceConfiguration persistenceConfiguration, Func<Type, string> tableNamingConvention)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Sagas.TableNamingConvention", tableNamingConvention);
        }
    }
}