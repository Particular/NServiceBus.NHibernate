namespace NServiceBus.Features
{
    using global::NHibernate.Cfg;
    using global::NHibernate.Mapping.ByCode;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;

    /// <summary>
    /// NHibernate Outbox Storage.
    /// </summary>
    public class NHibernateOutboxStorage : Feature
    {
        /// <summary>
        /// Creates an instance of <see cref="NHibernateOutboxStorage"/>.
        /// </summary>
        public NHibernateOutboxStorage()
        {
            DependsOn<Outbox>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<OutboxPersister>(DependencyLifecycle.SingleInstance);
        }

        internal static void ApplyMappings(Configuration config)
        {
            var mapper = new ModelMapper();
            mapper.AddMapping<OutboxEntityMap>();
            mapper.AddMapping<TransportOperationEntityMap>();

            config.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
        }
    }
}