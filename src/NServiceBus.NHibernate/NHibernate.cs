namespace NServiceBus.Persistence
{
    using Features;

    public class NHibernate:PersistenceDefinition
    {
         
    }

    class NHibernateConfigurer : IConfigurePersistence<NHibernate>
    {
        public void Configure()
        {
         
        }

        public void Enable(Configure config)
        {
            Feature.EnableByDefault<NHibernateStorageSession>();
            Feature.EnableByDefault<NHibernateOutboxStorage>();
            Feature.EnableByDefault<NHibernateSagaStorage>();
            Feature.EnableByDefault<NHibernateTimeoutStorage>();
        }
    }
}