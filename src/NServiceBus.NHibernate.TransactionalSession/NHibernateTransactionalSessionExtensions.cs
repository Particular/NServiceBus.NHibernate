namespace NServiceBus.TransactionalSession
{
    using System;
    using System.Configuration;
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
        public static PersistenceExtensions<NHibernatePersistence> EnableTransactionalSession(
            this PersistenceExtensions<NHibernatePersistence> persistenceExtensions,
            TransactionalSessionOptions transactionalSessionOptions)
        {
            ArgumentNullException.ThrowIfNull(persistenceExtensions);
            ArgumentNullException.ThrowIfNull(transactionalSessionOptions);

            var settings = persistenceExtensions.GetSettings();

            settings.Set(transactionalSessionOptions);
            settings.EnableFeatureByDefault<NHibernateTransactionalSession>();

            if (string.IsNullOrEmpty(transactionalSessionOptions.ProcessorAddress))
            {
                return persistenceExtensions;
            }

            // remote processor configured, so turn off the outbox cleanup on this instance
            ConfigurationManager.AppSettings.Set(
                "NServiceBus/Outbox/NHibernate/FrequencyToRunDeduplicationDataCleanup",
                Timeout.InfiniteTimeSpan.ToString());

            //set the endpoint name to be the processor address, this makes sure that the outbox uses this value when generate qualified message IDs.
            //By default, the endpoint name used is automatically managed and set to the originating endpoint's name unless overridden by setting a value to NHibernateOutbox.ProcessorEndpointKey
            settings.Set(NHibernateOutbox.ProcessorEndpointKey, transactionalSessionOptions.ProcessorAddress);

            // If a remote processor is configured, this endpoint should not create the outbox tables.
            settings.Set(NHibernateOutbox.DisableOutboxTableCreationSettingKey, true);

            return persistenceExtensions;
        }

        /// <summary>
        /// Opens the transactional session
        /// </summary>
        public static Task Open(this ITransactionalSession transactionalSession,
            CancellationToken cancellationToken = default)
            => transactionalSession.Open(new NHibernateOpenSessionOptions(), cancellationToken);
    }
}