namespace NServiceBus.Persistence
{
    using Features;

    class NHibernateConfigurer : IConfigurePersistence<NHibernate>
    {
        public void Enable(Configure config)
        {
            config
                .Features(f => f.Enable<NHibernateStorageSession>())
                .Features(f => f.Enable<NHibernateOutboxStorage>())
                .Features(f => f.Enable<NHibernateSagaStorage>())
                .Features(f => f.Enable<NHibernateSubscriptionStorage>())
                .Features(f => f.Enable<NHibernateTimeoutStorage>())
                .Features(f => f.Enable<NHibernateGatewayDeduplication>())
                ;
        }
    }
}