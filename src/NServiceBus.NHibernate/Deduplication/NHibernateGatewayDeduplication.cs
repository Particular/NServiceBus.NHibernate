namespace NServiceBus.Features
{
    using Deduplication.NHibernate.Config;
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

            context.Container.ConfigureComponent<Deduplication.NHibernate.Installer.Installer>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.Configuration, config.Configuration)
                .ConfigureProperty(x => x.RunInstaller, RunInstaller(context));

            context.Container.ConfigureComponent<Deduplication.NHibernate.GatewayDeduplication>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.SessionFactory, config.Configuration.BuildSessionFactory());
        }

        static bool RunInstaller(FeatureConfigurationContext context)
        {
            return context.Settings.Get<bool>(context.Settings.HasSetting("NHibernate.GatewayDeduplication.AutoUpdateSchema") 
                ? "NHibernate.GatewayDeduplication.AutoUpdateSchema" 
                : "NHibernate.Common.AutoUpdateSchema");
        }
    }
}