namespace NServiceBus.Persistence.NHibernate
{
    using Configuration.AdvanceExtensibility;
    using NServiceBus.Features;

    /// <summary>
    /// Deduplication configuration extensions.
    /// </summary>
    public static class SharedSessionConfig
    {
        /// <summary>
        /// Disables sharing a connection between SQL Server transport and NHibernate persistence. Intended to be used in conjunction with Outbox only.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        public static PersistenceExtentions<NHibernatePersistence> DoNotShareTransportConnection(this PersistenceExtentions<NHibernatePersistence> persistenceConfiguration)
        {
            persistenceConfiguration.GetSettings().Set(NHibernateStorageSession.ShareTransportConnectionSettingsKey, false);
            return persistenceConfiguration;
        }
    }
}