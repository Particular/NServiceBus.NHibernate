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
        [ObsoleteEx(
            Message = "NHibernate gateway persistence is deprecated. Use the new NServiceBus.Gateway.Sql dedicated package.",
            RemoveInVersion = "10.0.0",
            TreatAsErrorFromVersion = "9.0.0")]
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
        [ObsoleteEx(
            Message = "NHibernate gateway persistence is deprecated. Use the new NServiceBus.Gateway.Sql dedicated package.",
            RemoveInVersion = "10.0.0",
            TreatAsErrorFromVersion = "9.0.0")]
        public static PersistenceExtensions<NHibernatePersistence> UseGatewayDeduplicationConfiguration(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration, Configuration configuration)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.GatewayDeduplication.Configuration", configuration);
            return persistenceConfiguration;
        }
    }
}