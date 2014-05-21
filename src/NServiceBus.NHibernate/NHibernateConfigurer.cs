namespace NServiceBus.Persistence
{
    using Features;

    class NHibernateConfigurer : IConfigurePersistence<NHibernate>
    {
        public void Enable(Configure config)
        {
            config.Features.EnableByDefault<NHibernateStorageSession>();
            config.Features.EnableByDefault<NHibernateOutboxStorage>();
            config.Features.EnableByDefault<NHibernateSagaStorage>();
            config.Features.EnableByDefault<NHibernateSubscriptionStorage>();
            config.Features.EnableByDefault<NHibernateTimeoutStorage>();
            config.Features.EnableByDefault<NHibernateGatewayDeduplication>();
        }
    }
}