namespace NServiceBus.Features
{
    using Deduplication.NHibernate.Config;
    using NHibernate.Cfg;
    using Persistence.NHibernate;

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
            DependsOn("Gateway");
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var properties = new ConfigureNHibernate(context.Settings).GatewayDeduplicationProperties;

            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(properties);

            var configuration = context.Settings.GetOrDefault<Configuration>("NHibernate.GatewayDeduplication.Configuration") ?? context.Settings.GetOrDefault<Configuration>("StorageConfiguration");

            if (configuration == null)
            {
                configuration = new Configuration()
                    .SetProperties(properties);
            }

            ConfigureNHibernate.AddMappings<DeduplicationMessageMap>(configuration);

            Deduplication.NHibernate.Installer.Installer.configuration = configuration;

            if (context.Settings.HasSetting("NHibernate.GatewayDeduplication.AutoUpdateSchema"))
            {
                Deduplication.NHibernate.Installer.Installer.RunInstaller = context.Settings.Get<bool>("NHibernate.GatewayDeduplication.AutoUpdateSchema");
            }
            else
            {
                Deduplication.NHibernate.Installer.Installer.RunInstaller = context.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema");
            }

            context.Container.ConfigureComponent<Deduplication.NHibernate.GatewayDeduplication>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.SessionFactory, configuration.BuildSessionFactory());
        }
    }
}