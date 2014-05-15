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
    using TimeoutPersisters.NHibernate;
    using TimeoutPersisters.NHibernate.Config;

    public class NHibernateTimeoutStorage : Feature
    {
        public override bool IsEnabledByDefault
        {
            get { return true; }
        }

        public override bool ShouldBeEnabled()
        {
            return IsEnabled<TimeoutManager>();
        }

        public override void Initialize()
        {
            InitializeInner(Configure.Instance.Configurer);
        }

        void InitializeInner(IConfigureComponents config)
        {
            var properties = ConfigureNHibernate.TimeoutPersisterProperties;
            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(properties);

            var configuration = SettingsHolder.GetOrDefault<Configuration>("NHibernate.Timeouts.Configuration");

            if (configuration == null)
            {
                configuration = new Configuration()
                    .SetProperties(properties);
            }

            ConfigureNHibernate.AddMappings<TimeoutEntityMap>(configuration);

            TimeoutPersisters.NHibernate.Installer.Installer.configuration = configuration;

            TimeoutPersisters.NHibernate.Installer.Installer.RunInstaller = SettingsHolder.Get<bool>("NHibernate.Common.AutoUpdateSchema");

            if (SettingsHolder.HasSetting("NHibernate.Timeouts.AutoUpdateSchema"))
            {
                TimeoutPersisters.NHibernate.Installer.Installer.RunInstaller = SettingsHolder.Get<bool>("NHibernate.Timeouts.AutoUpdateSchema");
            }
            else
            {
                TimeoutPersisters.NHibernate.Installer.Installer.RunInstaller = SettingsHolder.Get<bool>("NHibernate.Common.AutoUpdateSchema");
            }

            config.ConfigureComponent<TimeoutStorage>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.SessionFactory, configuration.BuildSessionFactory());
        }

    }
}