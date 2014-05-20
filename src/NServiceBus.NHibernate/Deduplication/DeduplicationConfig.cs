namespace NServiceBus.NHibernate
{
    using global::NHibernate.Cfg;
    using Persistence;

    public static class DeduplicationConfig
    {
        public static void DisableGatewayDeduplicationSchemaUpdate(this PersistenceConfiguration persistenceConfiguration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.GatewayDeduplication.AutoUpdateSchema", false);
        }

        public static void UseGatewayDeduplicationConfiguration(this PersistenceConfiguration persistenceConfiguration, Configuration configuration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.GatewayDeduplication.Configuration", configuration);
        }

    }
}