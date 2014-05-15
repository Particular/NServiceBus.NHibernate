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
            Feature.EnableByDefault<NHibernateSessionManagement>();
            Feature.EnableByDefault<NHibernateOutbox>();
            Feature.EnableByDefault<NHibernateSagaPersistence>();
        }
    }
}