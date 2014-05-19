namespace NServiceBus.Features
{
    using global::NHibernate.Cfg;
    using NHibernate.Internal;
    using TimeoutPersisters.NHibernate;
    using TimeoutPersisters.NHibernate.Config;

    public class NHibernateTimeoutStorage : Feature
    {
        public NHibernateTimeoutStorage()
        {
            DependsOn<TimeoutManager>();
        }

        public override void Initialize(Configure config)
        {
            var properties = new ConfigureNHibernate(config.Settings)
                .TimeoutPersisterProperties;

            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(properties);

            var configuration = config.Settings.GetOrDefault<Configuration>("NHibernate.Timeouts.Configuration");

            if (configuration == null)
            {
                configuration = new Configuration()
                    .SetProperties(properties);
            }

            ConfigureNHibernate.AddMappings<TimeoutEntityMap>(configuration);

            TimeoutPersisters.NHibernate.Installer.Installer.configuration = configuration;

            if (config.Settings.HasSetting("NHibernate.Timeouts.AutoUpdateSchema"))
            {
                TimeoutPersisters.NHibernate.Installer.Installer.RunInstaller = config.Settings.Get<bool>("NHibernate.Timeouts.AutoUpdateSchema");
            }
            else
            {
                TimeoutPersisters.NHibernate.Installer.Installer.RunInstaller = config.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema");
            }

            config.Configurer.ConfigureComponent<TimeoutStorage>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.SessionFactory, configuration.BuildSessionFactory())
                .ConfigureProperty(p=>p.EndpointName,config.EndpointName);
        }

    }
}