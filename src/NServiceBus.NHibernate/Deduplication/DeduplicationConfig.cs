namespace NServiceBus.Persistence.NHibernate
{
    using Configuration.AdvanceExtensibility;
    using global::NHibernate.Cfg;

    /// <summary>
    /// Deduplication configuration extensions.
    /// </summary>
    public static class DeduplicationConfig
    {
        /// <summary>
        /// Disables automatic schema update.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        public static PersistenceExtentions<NHibernatePersistence> DisableGatewayDeduplicationSchemaUpdate(this PersistenceExtentions<NHibernatePersistence> persistenceConfiguration)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.GatewayDeduplication.AutoUpdateSchema", false);
            return persistenceConfiguration;
        }

        /// <summary>
        /// Configures Subscription Storage to use the <paramref name="configuration"/>.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        public static PersistenceExtentions<NHibernatePersistence> UseGatewayDeduplicationConfiguration(this PersistenceExtentions<NHibernatePersistence> persistenceConfiguration, Configuration configuration)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.GatewayDeduplication.Configuration", configuration);
            return persistenceConfiguration;
        }
    }
}