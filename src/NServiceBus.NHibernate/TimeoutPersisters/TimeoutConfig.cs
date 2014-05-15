namespace NServiceBus.NHibernate
{
    using global::NHibernate.Cfg;
    using Settings;

    public static class TimeoutConfig
    {
        public static void DisableTimeoutStorageSchemaUpdate(this PersistenceConfiguration config)
        {
            SettingsHolder.Set("NHibernate.Timeouts.AutoUpdateSchema", false);
        }

        public static void UseTimeoutStorageConfiguration(this PersistenceConfiguration config, Configuration configuration)
        {
            SettingsHolder.Set("NHibernate.Timeouts.Configuration", configuration);
        }

    }
}