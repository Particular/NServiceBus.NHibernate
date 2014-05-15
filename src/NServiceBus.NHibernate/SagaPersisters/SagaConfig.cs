namespace NServiceBus.NHibernate
{
    using System;
    using Settings;

    public static class SagaConfig
    {
        public static void SagaTableNamingConvention(this PersistenceConfiguration config, Func<Type, string> tableNamingConvention)
        {
            SettingsHolder.Set("NHibernate.Sagas.TableNamingConvention", tableNamingConvention);
        }
    }
}