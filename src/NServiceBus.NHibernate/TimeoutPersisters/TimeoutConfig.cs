namespace NServiceBus.NHibernate
{
    using global::NHibernate.Cfg;
    using Persistence;

    public static class TimeoutConfig
    {
        public static void DisableTimeoutStorageSchemaUpdate(this PersistenceConfiguration persistenceConfiguration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Timeouts.AutoUpdateSchema", false);
        }

        public static void UseTimeoutStorageConfiguration(this PersistenceConfiguration persistenceConfiguration, Configuration configuration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Timeouts.Configuration", configuration);
        }

    }
}