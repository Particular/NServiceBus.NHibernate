namespace NServiceBus.Persistence.NHibernate
{
    using Configuration.AdvanceExtensibility;
    using global::NHibernate.Cfg;

    //TODO

    /// <summary>
    /// Timeout configuration extensions.
    /// </summary>
    public static class TimeoutConfig
    {
        /// <summary>
        /// Disables automatic schema update.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        public static PersistenceExtentions<NHibernatePersistence> DisableTimeoutStorageSchemaUpdate(this PersistenceExtentions<NHibernatePersistence> persistenceConfiguration)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.Timeouts.AutoUpdateSchema", false);
            return persistenceConfiguration;
        }

        /// <summary>
        /// Configures Timeout Storage to use the <paramref name="configuration"/>.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        public static PersistenceExtentions<NHibernatePersistence> UseTimeoutStorageConfiguration(this PersistenceExtentions<NHibernatePersistence> persistenceConfiguration, Configuration configuration)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.Timeouts.Configuration", configuration);
            return persistenceConfiguration;
        }

    }
}