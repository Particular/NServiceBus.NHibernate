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
            //until we have a better hook in the feature base class
            Feature.EnableByDefault<NHibernateStorageSession>();
            Feature.EnableByDefault<NHibernateOutboxStorage>();
            Feature.EnableByDefault<NHibernateSagaStorage>();
        }
    }
}