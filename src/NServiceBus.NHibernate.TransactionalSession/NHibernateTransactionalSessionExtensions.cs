namespace NServiceBus.TransactionalSession
{
    using Configuration.AdvancedExtensibility;
    using Features;

    static class NHibernateTransactionalSessionExtensions
    {
        public static PersistenceExtensions<NHibernatePersistence> EnableTransactionalSession(
            this PersistenceExtensions<NHibernatePersistence> persistenceExtensions)
        {
            persistenceExtensions.GetSettings().EnableFeatureByDefault(typeof(NHibernateTransactionalSession));

            return persistenceExtensions;
        }
    }
}