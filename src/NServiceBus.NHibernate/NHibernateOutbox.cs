namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using global::NHibernate.Cfg;
    using global::NHibernate.Mapping.ByCode;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using ObjectBuilder;
    using Settings;

    public class NHibernateOutbox : Feature
    {
        public override bool IsEnabledByDefault
        {
            get { return true; }
        }

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