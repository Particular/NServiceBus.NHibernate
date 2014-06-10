namespace NServiceBus.Persistence.NHibernate
{
    using global::NHibernate.Cfg;
    using Persistence;

    /// <summary>
    /// Timeout configuration extensions.
    /// </summary>
    public static class TimeoutConfig
    {
        /// <summary>
        /// Disables automatic schema update.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        public static void DisableTimeoutStorageSchemaUpdate(this PersistenceConfiguration persistenceConfiguration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Timeouts.AutoUpdateSchema", false);
        }

        /// <summary>
        /// Configures Timeout Storage to use the <paramref name="configuration"/>.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        public static void UseTimeoutStorageConfiguration(this PersistenceConfiguration persistenceConfiguration, Configuration configuration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Timeouts.Configuration", configuration);
        }

    }
}