namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
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
            var mapper = new ModelMapper();
            mapper.AddMapping<OutboxEntityMap>();
            mapper.AddMapping<TransportOperationEntityMap>();

            config.Settings.Get<List<Action<Configuration>>>("StorageConfigurationModifications")
                .Add(c => c.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities()));

            config.Configurer.ConfigureComponent<OutboxPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}