namespace NServiceBus.TransactionalSession
{
    using Configuration.AdvancedExtensibility;
    using Features;

    /// <summary>
    /// Enables the transactional session feature.
    /// </summary>
    public static class NHibernateTransactionalSessionExtensions
    {
        /// <summary>
        /// Enables transactional session for this endpoint.
        /// </summary>
        public static PersistenceExtensions<NHibernatePersistence> EnableTransactionalSession(
            this PersistenceExtensions<NHibernatePersistence> persistenceExtensions)
        {
            persistenceExtensions.GetSettings().EnableFeatureByDefault(typeof(NHibernateTransactionalSession));

            return persistenceExtensions;
        }
    }
}