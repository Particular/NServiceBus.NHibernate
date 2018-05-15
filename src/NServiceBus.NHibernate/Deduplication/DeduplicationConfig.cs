namespace NServiceBus.Persistence.NHibernate
{
    using global::NHibernate.Cfg;
    using Configuration.AdvancedExtensibility;

    /// <summary>
    /// Deduplication configuration extensions.
    /// </summary>
    public static class DeduplicationConfig
    {
        /// <summary>
        /// Disables automatic schema update.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        public static PersistenceExtensions<NHibernatePersistence> DisableGatewayDeduplicationSchemaUpdate(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.GatewayDeduplication.AutoUpdateSchema", false);
            return persistenceConfiguration;
        }

        /// <summary>
        /// Configures Subscription Storage to use the <paramref name="configuration"/>.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        public static PersistenceExtensions<NHibernatePersistence> UseGatewayDeduplicationConfiguration(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration, Configuration configuration)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.GatewayDeduplication.Configuration", configuration);
            return persistenceConfiguration;
        }
    }
}