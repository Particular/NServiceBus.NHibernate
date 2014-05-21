namespace NServiceBus.Features
{
    using global::NHibernate.Cfg;
    using NHibernate.Internal;
    using TimeoutPersisters.NHibernate;
    using TimeoutPersisters.NHibernate.Config;

    /// <summary>
    /// NHibernate Timeout Storage.
    /// </summary>
    public class NHibernateTimeoutStorage : Feature
    {
        /// <summary>
        /// Creates an instance of <see cref="NHibernateTimeoutStorage"/>.
        /// </summary>
        public NHibernateTimeoutStorage()
        {
            DependsOn<TimeoutManager>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
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