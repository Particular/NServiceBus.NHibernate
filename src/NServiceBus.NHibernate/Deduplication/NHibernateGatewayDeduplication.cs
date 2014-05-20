namespace NServiceBus.Features
{
    using Deduplication.NHibernate.Config;
    using global::NHibernate.Cfg;
    using NHibernate.Internal;

    public class NHibernateGatewayDeduplication : Feature
    {
        public NHibernateGatewayDeduplication()
        {
            DependsOn<Gateway>();
        }

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