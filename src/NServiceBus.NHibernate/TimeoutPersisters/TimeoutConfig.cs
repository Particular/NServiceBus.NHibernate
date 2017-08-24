namespace NServiceBus.Persistence.NHibernate
{
    using global::NHibernate.Cfg;
    using Configuration.AdvancedExtensibility;

    /// <summary>
    /// Timeout configuration extensions.
    /// </summary>
    public static class TimeoutConfig
    {
        /// <summary>
        /// Disables automatic schema update.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        public static PersistenceExtensions<NHibernatePersistence> DisableTimeoutStorageSchemaUpdate(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.Timeouts.AutoUpdateSchema", false);
            return persistenceConfiguration;
        }

        /// <summary>
        /// Configures Timeout Storage to use the <paramref name="configuration"/>.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        public static PersistenceExtensions<NHibernatePersistence> UseTimeoutStorageConfiguration(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration, Configuration configuration)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.Timeouts.Configuration", configuration);
            return persistenceConfiguration;
        }

    }
}