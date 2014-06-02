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
        /// 
        protected override void Setup(FeatureConfigurationContext context)
        {
            var properties = new ConfigureNHibernate(context.Settings)
                .TimeoutPersisterProperties;

            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(properties);

            var configuration = context.Settings.GetOrDefault<Configuration>("NHibernate.Timeouts.Configuration");

            if (configuration == null)
            {
                configuration = new Configuration()
                    .SetProperties(properties);
            }

            ConfigureNHibernate.AddMappings<TimeoutEntityMap>(configuration);

            TimeoutPersisters.NHibernate.Installer.Installer.configuration = configuration;

            if (context.Settings.HasSetting("NHibernate.Timeouts.AutoUpdateSchema"))
            {
                TimeoutPersisters.NHibernate.Installer.Installer.RunInstaller = context.Settings.Get<bool>("NHibernate.Timeouts.AutoUpdateSchema");
            }
            else
            {
                TimeoutPersisters.NHibernate.Installer.Installer.RunInstaller = context.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema");
            }

            context.Container.ConfigureComponent<TimeoutStorage>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.SessionFactory, configuration.BuildSessionFactory())
                .ConfigureProperty(p=>p.EndpointName, context.Settings.EndpointName());
        }

    }
}