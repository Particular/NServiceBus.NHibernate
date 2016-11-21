namespace NServiceBus.NHibernate.Outbox
{
    using global::NHibernate.Mapping.ByCode.Conformist;
    using Configuration.AdvanceExtensibility;
    using NServiceBus.Outbox.NHibernate;

    /// <summary>
    /// Outbox configuration extensions.
    /// </summary>
    public static class OutboxConfig
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        public static PersistenceExtensions<NHibernatePersistence> UseOutboxRecord<TEntity, TMapping>(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration)
            where TEntity : class, IOutboxRecord, new()
            where TMapping : ClassMapping<TEntity>
        {
            persistenceConfiguration.GetSettings().Set<IOutboxPersisterFactory>(new OutboxPersisterFactory<TEntity>());
            persistenceConfiguration.GetSettings().Set("NServiceBus.NHibernate.OutboxMapping", typeof(TMapping));
            return persistenceConfiguration;
        }
    }
}