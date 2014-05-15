namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using global::NHibernate.Cfg;
    using global::NHibernate.Mapping.ByCode;
    using NHibernate.Internal;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using ObjectBuilder;
    using Settings;

    public class NHibernateOutbox : Feature
    {
        public override bool ShouldBeEnabled()
        {
            if (!IsEnabled<Outbox>())
            {
                return false;
            }

            var mapper = new ModelMapper();
            mapper.AddMapping<OutboxEntityMap>();
            mapper.AddMapping<TransportOperationEntityMap>();

            SettingsHolder.Get<List<Action<Configuration>>>("StorageConfigurationModifications")
                .Add(c => c.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities()));

            foreach (var kvp in ConfigureNHibernate.OutboxProperties)
            {
                SettingsHolder.Get<Dictionary<string, string>>("StorageProperties")[kvp.Key] = kvp.Value;

            }

            return true;
        }

        public override void Initialize()
        {
            InitializeInner(Configure.Instance.Configurer);
        }

        void InitializeInner(IConfigureComponents config)
        {
            config.ConfigureComponent<OutboxPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}