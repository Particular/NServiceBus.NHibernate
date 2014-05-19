namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using global::NHibernate.Cfg;
    using global::NHibernate.Mapping.ByCode;
    using NHibernate.Internal;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using Settings;

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

            foreach (var kvp in new ConfigureNHibernate(new SettingsHolder()).OutboxProperties)
            {
                config.Settings.Get<Dictionary<string, string>>("StorageProperties")[kvp.Key] = kvp.Value;
            }

            config.Configurer.ConfigureComponent<OutboxPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}