namespace NServiceBus.NHibernate.Outbox
{
    using System;
    using global::NHibernate.Mapping.ByCode.Conformist;
    using Configuration.AdvanceExtensibility;
    using NServiceBus.Features;
    using NServiceBus.Outbox.NHibernate;

    /// <summary>
    /// Outbox configuration extensions.
    /// </summary>
    public static class OutboxConfig
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="persistenceConfiguration">The NHibernate persister configuration instance.</param>
        /// <typeparam name="TEntity">Outbox record entity type.</typeparam>
        /// <typeparam name="TMapping">Outbox record entity type class mapping type.</typeparam>
        /// <returns>The NHibernate configuration.</returns>
        public static PersistenceExtensions<NHibernatePersistence> UseOutboxRecord<TEntity, TMapping>(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration)
            where TEntity : class, IOutboxRecord, new()
            where TMapping : ClassMapping<TEntity>
        {
            persistenceConfiguration.GetSettings().Set<IOutboxPersisterFactory>(new OutboxPersisterFactory<TEntity>());
            persistenceConfiguration.GetSettings().Set(NHibernateStorageSession.OutboxMappingSettingsKey, typeof(TMapping));
            return persistenceConfiguration;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="persistenceConfiguration">The NHibernate persister configuration instance.</param>
        /// <param name="outboxTableName">Table name to use for outbox records.</param>
        /// <returns>The NHibernate configuration.</returns>
        public static PersistenceExtensions<NHibernatePersistence> UseOutboxTableName(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration, string outboxTableName)
        {
            persistenceConfiguration.GetSettings().Set(NHibernateStorageSession.OutboxTableNameSettingsKey, outboxTableName);
            return persistenceConfiguration;
        }

        /// <summary>
        /// Assign a custom schema name for the outbox record.
        /// </summary>
        /// <param name="persistenceConfiguration">The NHibernate persister configuration instance.</param>
        /// <param name="outboxSchemaName">Schema to use for outbox table.</param>
        /// <returns>The NHibernate configuration.</returns>
        public static PersistenceExtensions<NHibernatePersistence> UseOutboxSchemaName(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration, string outboxSchemaName)
        {
            persistenceConfiguration.GetSettings().Set(NHibernateStorageSession.OutboxSchemaNameSettingsKey, outboxSchemaName);
            return persistenceConfiguration;
        }
/* TODO: WIP
        /// <summary>
        /// Sets the time to keep the deduplication data to the specified time span.
        /// </summary>
        /// <param name="persistenceConfiguration">The NHibernate persister configuration instance.</param>
        /// <param name="timeToKeepDeduplicationData">The time to keep the deduplication data. 
        /// The cleanup process removes entries older than the specified time to keep deduplication data, therefore the time span cannot be negative</param>
        /// <returns>The NHibernate configuration.</returns>
        public static PersistenceExtensions<NHibernatePersistence> SetTimeToKeepDeduplicationData(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration, TimeSpan timeToKeepDeduplicationData)
        {
            persistenceConfiguration.GetSettings().Set(NHibernateStorageSession.OutboxTimeToKeepDeduplicationDataSettingsKey, timeToKeepDeduplicationData);
            return persistenceConfiguration;
        }


        /// <summary>
        /// Sets the frequency to run the deduplication data cleanup task.
        /// </summary>
        /// <param name="persistenceConfiguration">The NHibernate persister configuration instance.</param>
        /// <param name="frequencyToRunDeduplicationDataCleanup">The frequency to run the deduplication data cleanup task. By specifying a negative time span (-1) the cleanup task will never run.</param>
        /// <returns>The NHibernate configuration.</returns>
        public static PersistenceExtensions<NHibernatePersistence> SetFrequencyToRunDeduplicationDataCleanup(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration, TimeSpan frequencyToRunDeduplicationDataCleanup)
        {
            persistenceConfiguration.GetSettings().Set(NHibernateStorageSession.OutboxCleanupIntervalSettingsKey, frequencyToRunDeduplicationDataCleanup);
            return persistenceConfiguration;
        }
*/
    }
}
