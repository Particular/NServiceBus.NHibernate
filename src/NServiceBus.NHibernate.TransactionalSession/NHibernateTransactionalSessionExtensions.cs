namespace NServiceBus.TransactionalSession
{
    using System;
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
            this PersistenceExtensions<NHibernatePersistence> persistenceExtensions) =>
            EnableTransactionalSession(persistenceExtensions, new TransactionalSessionOptions());

        /// <summary>
        /// Enables the transactional session for this endpoint using the specified TransactionalSessionOptions.
        /// </summary>
        public static PersistenceExtensions<NHibernatePersistence> EnableTransactionalSession(this PersistenceExtensions<NHibernatePersistence> persistenceExtensions,
            TransactionalSessionOptions transactionalSessionOptions)
        {
            ArgumentNullException.ThrowIfNull(persistenceExtensions);
            ArgumentNullException.ThrowIfNull(transactionalSessionOptions);

            var settings = persistenceExtensions.GetSettings();

            settings.Set(transactionalSessionOptions);
            settings.EnableFeatureByDefault<NHibernateTransactionalSession>();

            return persistenceExtensions;
        }

        /// <summary>
        /// Opens the transactional session
        /// </summary>
        public static Task Open(this ITransactionalSession transactionalSession, CancellationToken cancellationToken = default)
            => transactionalSession.Open(new NHibernateOpenSessionOptions(), cancellationToken);
    }
}