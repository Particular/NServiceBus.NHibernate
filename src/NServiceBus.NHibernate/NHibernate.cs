namespace NServiceBus.Persistence
{
    public class NHibernate:PersistenceDefinition
    {
         
    }

    class NHibernateConfigurer : IConfigurePersistence<NHibernate>
    {
        public void Configure()
        {
            //until we have a better hook in the feature base class
            //Feature.Enable<NHibernateSessionManagement>();
            //Feature.Enable<NHibernateOutbox>();
            //Feature.Enable<NHibernateSagaPersistence>();
        }
    }
}