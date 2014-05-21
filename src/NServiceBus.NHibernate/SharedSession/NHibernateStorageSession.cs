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
        public override bool ShouldBeEnabled(Configure config)
        {
            return IsEnabled<NHibernateSagaStorage>() || IsEnabled<NHibernateOutboxStorage>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        public override void Initialize(Configure config)
        {
            var configuration = config.Settings.GetOrDefault<Configuration>("StorageConfiguration");

            if (configuration == null)
            {
                var properties = new ConfigureNHibernate(config.Settings).SagaPersisterProperties;

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

            config.Pipeline.Register<OpenSqlConnectionBehavior.Registration>();
            config.Pipeline.Register<OpenSessionBehavior.Registration>();
            config.Pipeline.Register<OpenNativeTransactionBehavior.Registration>();

            config.Configurer.RegisterSingleton<ISessionFactory>(configuration.BuildSessionFactory());

            config.Configurer.ConfigureComponent<StorageSessionProvider>(DependencyLifecycle.InstancePerCall)
                     .ConfigureProperty(p => p.ConnectionString, connString);

            config.Configurer.ConfigureComponent<DbConnectionProvider>(DependencyLifecycle.InstancePerCall)
                     .ConfigureProperty(p => p.ConnectionString, connString);

            config.Configurer.ConfigureComponent<OpenSqlConnectionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.ConnectionString, connString);

            config.Configurer.ConfigureComponent<OpenSessionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.ConnectionString, connString);

            config.Configurer.ConfigureComponent<OpenNativeTransactionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.ConnectionString, connString);

            Installer.RunInstaller = config.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema");

            Installer.configuration = configuration;
        }
    }
}