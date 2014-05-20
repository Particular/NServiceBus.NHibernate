namespace NServiceBus.Features
{
    using global::NHibernate.Cfg;
    using global::NHibernate.Mapping.ByCode;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;

    public class NHibernateOutboxStorage : Feature
    {
        public NHibernateOutboxStorage()
        {
            DependsOn<Outbox>();
        }

        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<OutboxPersister>(DependencyLifecycle.SingleInstance);
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