namespace NServiceBus.Features
{
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
            var configuration = context.Settings.GetOrDefault<Configuration>("NHibernate.Timeouts.Configuration") ?? context.Settings.GetOrDefault<Configuration>("StorageConfiguration");

            if (configuration == null)
            {
                var properties = new ConfigureNHibernate(context.Settings).TimeoutPersisterProperties;
                configuration = new Configuration().SetProperties(properties);
            }
            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(configuration.Properties);

            ConfigureNHibernate.AddMappings<TimeoutEntityMap>(configuration);

            context.Container.ConfigureComponent<TimeoutPersisters.NHibernate.Installer.Installer>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.Configuration, configuration)
                .ConfigureProperty(x => x.RunInstaller, RunInstaller(context));
                

            string connString;

            if (!configuration.Properties.TryGetValue(Environment.ConnectionString, out connString))
            {
                string connStringName;

                if (configuration.Properties.TryGetValue(Environment.ConnectionStringName, out connStringName))
                {
                    var connectionStringSettings = System.Configuration.ConfigurationManager.ConnectionStrings[connStringName];

                    connString = connectionStringSettings.ConnectionString;
                }
            }

            context.Container.ConfigureComponent<TimeoutPersister>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.ConnectionString, connString)
                .ConfigureProperty(p => p.SessionFactory, configuration.BuildSessionFactory())
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