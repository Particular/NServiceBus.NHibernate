namespace NServiceBus.Persistence
{
    using Features;

    class NHibernateConfigurer : IConfigurePersistence<NServiceBus.NHibernate>
    {
        public void Enable(Configure config)
        {
            config.Settings.SetDefault("NHibernate.Common.AutoUpdateSchema", true);

            config.Settings.EnableFeatureByDefault<NHibernateDBConnectionProvider>();
            config.Settings.EnableFeatureByDefault<NHibernateStorageSession>();
            config.Settings.EnableFeatureByDefault<NHibernateOutboxStorage>();
            config.Settings.EnableFeatureByDefault<NHibernateSagaStorage>();
            config.Settings.EnableFeatureByDefault<NHibernateSubscriptionStorage>();
            config.Settings.EnableFeatureByDefault<NHibernateTimeoutStorage>();
            config.Settings.EnableFeatureByDefault<NHibernateGatewayDeduplication>();
        }
    }
}