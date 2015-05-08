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
            var configuration = context.Settings.GetOrDefault<Configuration>("NHibernate.GatewayDeduplication.Configuration") ?? context.Settings.GetOrDefault<Configuration>("StorageConfiguration");

            if (configuration == null)
            {
                var properties = new ConfigureNHibernate(context.Settings).GatewayDeduplicationProperties;
                configuration = new Configuration().SetProperties(properties);
            }
            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(configuration.Properties);

            ConfigureNHibernate.AddMappings<DeduplicationMessageMap>(configuration);

            context.Container.ConfigureComponent<Deduplication.NHibernate.Installer.Installer>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.Configuration, configuration)
                .ConfigureProperty(x => x.RunInstaller, RunInstaller(context));

            context.Container.ConfigureComponent<Deduplication.NHibernate.GatewayDeduplication>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.SessionFactory, configuration.BuildSessionFactory());
        }

        static bool RunInstaller(FeatureConfigurationContext context)
        {
            return context.Settings.Get<bool>(context.Settings.HasSetting("NHibernate.GatewayDeduplication.AutoUpdateSchema") 
                ? "NHibernate.GatewayDeduplication.AutoUpdateSchema" 
                : "NHibernate.Common.AutoUpdateSchema");
        }
    }
}