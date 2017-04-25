namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using Deduplication.NHibernate.Config;
    using Deduplication.NHibernate;
    using TimeoutPersisters.NHibernate.Installer;
    using Persistence.NHibernate;
    using Installer = Deduplication.NHibernate.Installer.Installer;

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
            DependsOn("NServiceBus.Features.Gateway");

            // since the installers are registered even if the feature isn't enabled we need to make 
            // this a no-op of there is no "schema updater" available
            Defaults(c => c.Set<Installer.SchemaUpdater>(new Installer.SchemaUpdater()));
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var builder = new NHibernateConfigurationBuilder(context.Settings, "Deduplication", "NHibernate.GatewayDeduplication.Configuration", "StorageConfiguration");
            builder.AddMappings<DeduplicationMessageMap>();
            var config = builder.Build();

            if (RunInstaller(context))
            {
                context.Settings.Get<Installer.SchemaUpdater>().Execute = identity =>
                {
                    new OptimizedSchemaUpdate(config.Configuration).Execute(false, true);

                    return Task.FromResult(0);
                };
            }
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