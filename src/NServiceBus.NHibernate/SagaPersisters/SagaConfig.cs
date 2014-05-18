namespace NServiceBus.NHibernate
{
    using System;
    using Persistence;
    using Settings;

    public static class SagaConfig
    {
        public static void SagaTableNamingConvention(this PersistenceConfiguration config, Func<Type, string> tableNamingConvention)
        {
            SettingsHolder.Instance.Set("NHibernate.Sagas.TableNamingConvention", tableNamingConvention);
        }
    }
}