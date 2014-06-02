namespace NServiceBus.Features
{
    using NHibernate.Internal;
    using NHibernate.SharedSession;
    using global::NHibernate.Cfg;
    using global::NHibernate;
    using Environment = global::NHibernate.Cfg.Environment;

    /// <summary>
    /// NHibernate Storage Session.
    /// </summary>
    public class NHibernateStorageSession : Feature
    {
        /// <summary>
        /// Returns true if the feature should be enable. This method wont be called if the feature is explicitly disabled
        /// </summary>
        /// <param name="config"/>
        public bool ShouldBeEnabled(Configure config)
        {
            return IsEnabled<NHibernateSagaStorage>() || IsEnabled<NHibernateOutboxStorage>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var configuration = context.Settings.GetOrDefault<Configuration>("StorageConfiguration");

            if (configuration == null)
            {
                var properties = new ConfigureNHibernate(context.Settings).SagaPersisterProperties;

                configuration = new Configuration()
                    .SetProperties(properties);
            }
            
            if (IsEnabled<NHibernateOutboxStorage>())
            {
                NHibernateOutboxStorage.ApplyMappings(configuration);
            }

            if (IsEnabled<NHibernateSagaStorage>())
            {
                NHibernateSagaStorage.ApplyMappings(config, configuration);
            }

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

            context.Pipeline.Register<OpenSqlConnectionBehavior.Registration>();
            context.Pipeline.Register<OpenSessionBehavior.Registration>();
            context.Pipeline.Register<OpenNativeTransactionBehavior.Registration>();

            context.Container.RegisterSingleton<ISessionFactory>(configuration.BuildSessionFactory());

            context.Container.ConfigureComponent<StorageSessionProvider>(DependencyLifecycle.InstancePerCall)
                     .ConfigureProperty(p => p.ConnectionString, connString);

            context.Container.ConfigureComponent<DbConnectionProvider>(DependencyLifecycle.InstancePerCall)
                     .ConfigureProperty(p => p.ConnectionString, connString);

            context.Container.ConfigureComponent<OpenSqlConnectionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.ConnectionString, connString);

            context.Container.ConfigureComponent<OpenSessionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.ConnectionString, connString);

            context.Container.ConfigureComponent<OpenNativeTransactionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.ConnectionString, connString);

            Installer.RunInstaller = context.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema");

            Installer.configuration = configuration;
        }
    }
}