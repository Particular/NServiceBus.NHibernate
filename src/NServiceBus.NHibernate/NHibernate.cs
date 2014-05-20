namespace NServiceBus.Persistence
{
    using Features;

    public class NHibernate : PersistenceDefinition
    {

    }

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