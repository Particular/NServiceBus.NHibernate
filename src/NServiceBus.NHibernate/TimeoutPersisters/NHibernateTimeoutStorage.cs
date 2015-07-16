namespace NServiceBus.Features
{
    using System.Configuration;
    using NHibernate.Cfg;
    using Persistence.NHibernate;
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
            DependsOn<NHibernateDBConnectionProvider>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var builder = new NHibernateConfigurationBuilder(context.Settings, "Timeout", "NHibernate.Timeouts.Configuration", "StorageConfiguration");
            builder.AddMappings<TimeoutEntityMap>();
            var config = builder.Build();

            context.Container.ConfigureComponent<TimeoutPersisters.NHibernate.Installer.Installer>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.Configuration, config.Configuration)
                .ConfigureProperty(x => x.RunInstaller, RunInstaller(context));

            context.Container.ConfigureComponent<TimeoutPersister>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.ConnectionString, config.ConnectionString)
                .ConfigureProperty(p => p.SessionFactory, config.Configuration.BuildSessionFactory())
                .ConfigureProperty(p => p.EndpointName, context.Settings.EndpointName());
        }

        static bool RunInstaller(FeatureConfigurationContext context)
        {
            return context.Settings.Get<bool>(context.Settings.HasSetting("NHibernate.Timeouts.AutoUpdateSchema") 
                ? "NHibernate.Timeouts.AutoUpdateSchema" 
                : "NHibernate.Common.AutoUpdateSchema");
        }
    }
}