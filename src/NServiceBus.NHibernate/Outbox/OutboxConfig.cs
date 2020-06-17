namespace NServiceBus.NHibernate.Outbox
{
    using global::NHibernate.Mapping.ByCode.Conformist;
    using Configuration.AdvancedExtensibility;
    using Features;
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
        /// <param name="outboxSchemaName">Schema to use for the outbox table (optional, defaults to the default schema).</param>
        /// <returns>The NHibernate configuration.</returns>
        public static PersistenceExtensions<NHibernatePersistence> CustomizeOutboxTableName(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration, string outboxTableName, string outboxSchemaName = null)
        {
            persistenceConfiguration.GetSettings().Set(NHibernateStorageSession.OutboxTableNameSettingsKey, outboxTableName);
            persistenceConfiguration.GetSettings().Set(NHibernateStorageSession.OutboxSchemaNameSettingsKey, outboxSchemaName);
            return persistenceConfiguration;
        }
    }
}