namespace NServiceBus.NHibernate
{
    using global::NHibernate.Cfg;
    using Persistence;

    /// <summary>
    /// Deduplication configuration extensions.
    /// </summary>
    public static class DeduplicationConfig
    {
        /// <summary>
        /// Disables automatic schema update.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        public static void DisableGatewayDeduplicationSchemaUpdate(this PersistenceConfiguration persistenceConfiguration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.GatewayDeduplication.AutoUpdateSchema", false);
        }

        /// <summary>
        /// Configures Subscription Storage to use the <paramref name="configuration"/>.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        public static void UseGatewayDeduplicationConfiguration(this PersistenceConfiguration persistenceConfiguration, Configuration configuration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.GatewayDeduplication.Configuration", configuration);
        }

    }
}