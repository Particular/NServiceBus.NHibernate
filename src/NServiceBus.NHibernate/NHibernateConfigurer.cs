namespace NServiceBus.Persistence
{
    using Features;

    class NHibernateConfigurer : IConfigurePersistence<NHibernate>
    {
        public void Enable(Configure config)
        {
            config.Settings.EnableFeatureByDefault<NHibernateStorageSession>();
            config.Settings.EnableFeatureByDefault<NHibernateOutboxStorage>();
            config.Settings.EnableFeatureByDefault<NHibernateSagaStorage>();
            config.Settings.EnableFeatureByDefault<NHibernateSubscriptionStorage>();
            config.Settings.EnableFeatureByDefault<NHibernateTimeoutStorage>();
            config.Settings.EnableFeatureByDefault<NHibernateGatewayDeduplication>();
        }
    }
}