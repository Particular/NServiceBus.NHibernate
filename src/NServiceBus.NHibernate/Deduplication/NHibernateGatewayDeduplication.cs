namespace NServiceBus.Features
{
    using Deduplication.NHibernate.Config;
    using global::NHibernate.Cfg;
    using NHibernate.Internal;

    /// <summary>
    /// NHibernate Gateway Deduplication.
    /// </summary>
    public class NHibernateGatewayDeduplication : Feature
    {
        /// <summary>
        /// Creates an instance of <see cref="NHibernateGatewayDeduplication"/>.
        /// </summary>
        public NHibernateGatewayDeduplication()
        {
            DependsOn<Gateway>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        public override void Initialize(Configure config)
        {
            var properties = new ConfigureNHibernate(config.Settings).GatewayDeduplicationProperties;

            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(properties);

            var configuration = config.Settings.GetOrDefault<Configuration>("NHibernate.GatewayDeduplication.Configuration");

            if (configuration == null)
            {
                configuration = new Configuration()
                    .SetProperties(properties);
            }

            ConfigureNHibernate.AddMappings<DeduplicationMessageMap>(configuration);

            Deduplication.NHibernate.Installer.Installer.configuration = configuration;

            if (config.Settings.HasSetting("NHibernate.GatewayDeduplication.AutoUpdateSchema"))
            {
                Deduplication.NHibernate.Installer.Installer.RunInstaller = config.Settings.Get<bool>("NHibernate.GatewayDeduplication.AutoUpdateSchema");
            }
            else
            {
                Deduplication.NHibernate.Installer.Installer.RunInstaller = config.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema");
            }

            config.Configurer.ConfigureComponent<Deduplication.NHibernate.GatewayDeduplication>(
                DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.SessionFactory, configuration.BuildSessionFactory());
        }
    }
}