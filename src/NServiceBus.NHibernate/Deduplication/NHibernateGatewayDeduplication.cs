namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using Deduplication.NHibernate.Config;
    using Deduplication.NHibernate;
    using Deduplication.NHibernate.Installer;
    using global::NHibernate.Tool.hbm2ddl;
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
            DependsOn("NServiceBus.Features.Gateway");
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var builder = new NHibernateConfigurationBuilder(context.Settings, "Deduplication", "NHibernate.GatewayDeduplication.Configuration", "StorageConfiguration");
            builder.AddMappings<DeduplicationMessageMap>();
            var config = builder.Build();

            Func<string, Task> installAction = _ => Task.FromResult(0);

            if (RunInstaller(context))
            {
                installAction = identity =>
                {
                    new SchemaUpdate(config.Configuration).Execute(false, true);

                    return Task.FromResult(0);
                };
            }
            context.Container.ConfigureComponent(b => new Installer.SchemaUpdater(installAction), DependencyLifecycle.SingleInstance);
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