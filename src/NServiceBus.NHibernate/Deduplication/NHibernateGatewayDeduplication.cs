namespace NServiceBus.Features
{
    using Deduplication.NHibernate.Config;
    using Deduplication.NHibernate;
    using Deduplication.NHibernate.Installer;
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
            var builder = new NHibernateConfigurationBuilder(context.Settings, "Deduplication", "NHibernate.GatewayDeduplication.Configuration", "StorageConfiguration");
            builder.AddMappings<DeduplicationMessageMap>();
            var config = builder.Build();

            context.Container.ConfigureComponent<Installer>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.Configuration, RunInstaller(context) ? new Installer.ConfigWrapper(config.Configuration) : null);

            context.Container.ConfigureComponent(b => new GatewayDeduplication(config.Configuration.BuildSessionFactory()), DependencyLifecycle.SingleInstance);
        }

        static bool RunInstaller(FeatureConfigurationContext context)
        {
            return context.Settings.Get<bool>(context.Settings.HasSetting("NHibernate.GatewayDeduplication.AutoUpdateSchema") 
                ? "NHibernate.GatewayDeduplication.AutoUpdateSchema" 
                : "NHibernate.Common.AutoUpdateSchema");
        }
    }
}