namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using global::NHibernate.Cfg;

    /// <summary>
    /// Deduplication configuration extensions.
    /// </summary>
    public static class DeduplicationConfig
    {
        /// <summary>
        /// Disables automatic schema update.
        /// </summary>
        [ObsoleteEx(
            Message = "NHibernate gateway persistence is deprecated. Use the new NServiceBus.Gateway.Sql dedicated package.",
            RemoveInVersion = "10.0.0",
            TreatAsErrorFromVersion = "9.0.0")]
        public static PersistenceExtensions<NHibernatePersistence> DisableGatewayDeduplicationSchemaUpdate(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration)
        {
            throw new NotImplementedException("NHibernate gateway persistence is deprecated. Use the new NServiceBus.Gateway.Sql dedicated package.");
        }

        /// <summary>
        /// Configures Subscription Storage to use the <paramref name="configuration"/>.
        /// </summary>
        /// <param name="persistenceConfiguration">The persistence config object</param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        [ObsoleteEx(
            Message = "NHibernate gateway persistence is deprecated. Use the new NServiceBus.Gateway.Sql dedicated package.",
            RemoveInVersion = "10.0.0",
            TreatAsErrorFromVersion = "9.0.0")]
        public static PersistenceExtensions<NHibernatePersistence> UseGatewayDeduplicationConfiguration(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration, Configuration configuration)
        {
            throw new NotImplementedException("NHibernate gateway persistence is deprecated. Use the new NServiceBus.Gateway.Sql dedicated package.");
        }
    }
}