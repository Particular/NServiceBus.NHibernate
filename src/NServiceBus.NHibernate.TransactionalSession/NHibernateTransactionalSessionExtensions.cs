namespace NServiceBus.TransactionalSession
{
    using System.Threading;
    using System.Threading.Tasks;
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
            persistenceExtensions.GetSettings().EnableFeatureByDefault(typeof(TransactionalSession));
            persistenceExtensions.GetSettings().EnableFeatureByDefault(typeof(NHibernateTransactionalSession));

            return persistenceExtensions;
        }

        /// <summary>
        /// Opens the transactional session
        /// </summary>
        public static Task Open(this ITransactionalSession transactionalSession, CancellationToken cancellationToken = default)
            => transactionalSession.Open(new NHibernateOpenSessionOptions(), cancellationToken);
    }
}