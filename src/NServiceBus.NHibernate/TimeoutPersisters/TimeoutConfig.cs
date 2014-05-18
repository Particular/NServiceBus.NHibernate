namespace NServiceBus.NHibernate
{
    using global::NHibernate.Cfg;
    using Persistence;
    using Settings;

    public static class TimeoutConfig
    {
        public static void DisableTimeoutStorageSchemaUpdate(this PersistenceConfiguration config)
        {
            SettingsHolder.Instance.Set("NHibernate.Timeouts.AutoUpdateSchema", false);
        }

        public static void UseTimeoutStorageConfiguration(this PersistenceConfiguration config, Configuration configuration)
        {
            SettingsHolder.Instance.Set("NHibernate.Timeouts.Configuration", configuration);
        }

    }
}