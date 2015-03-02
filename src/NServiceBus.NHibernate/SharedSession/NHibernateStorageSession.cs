namespace NServiceBus.Features
{
    using NHibernate.Cfg;
    using NServiceBus.Outbox;
    using Persistence.NHibernate;
    using Pipeline;
    using Environment = NHibernate.Cfg.Environment;

    /// <summary>
    /// NHibernate Storage Session.
    /// </summary>
    public class NHibernateStorageSession : Feature
    {
        internal NHibernateStorageSession()
        {
            Defaults(s => s.SetDefault<SharedMappings>(new SharedMappings()));
            
            DependsOn<NHibernateDBConnectionProvider>();
            DependsOnAtLeastOne(typeof(NHibernateSagaStorage), typeof(NHibernateOutboxStorage));
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

            context.Settings.Get<SharedMappings>()
                .ApplyTo(configuration);

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

            context.Container.RegisterSingleton(new SessionFactoryProvider(configuration.BuildSessionFactory()));

            //When outbox is enbaled, do not share transport connection.
            if (context.Container.HasComponent<OutboxPersister>())
            {
                context.Container.ConfigureComponent<NonSharedConnectionStorageSessionProvider>(DependencyLifecycle.SingleInstance);
                context.Container.ConfigureProperty<DbConnectionProvider>(p => p.DisableConnectionSharing, true);
            }
            else
            {
                context.Pipeline.Register<OpenSqlConnectionBehavior.Registration>();
                context.Pipeline.Register<OpenSessionBehavior.Registration>();
                context.Pipeline.Register<OpenNativeTransactionBehavior.Registration>();
                context.Container.ConfigureProperty<DbConnectionProvider>(p => p.DefaultConnectionString, connString);
                context.Container.ConfigureComponent<OpenSqlConnectionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.ConnectionString, connString);
                context.Container.ConfigureComponent<OpenSessionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.ConnectionString, connString);
                context.Container.ConfigureComponent<OpenNativeTransactionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.ConnectionString, connString);
                context.Container.ConfigureComponent(b => new NHibernateStorageContext(b.Build<PipelineExecutor>(), connString), DependencyLifecycle.InstancePerUnitOfWork);
                context.Container.ConfigureComponent<SharedConnectionStorageSessionProvider>(DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(p => p.ConnectionString, connString);
            }

            Installer.RunInstaller = context.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema");
            Installer.configuration = configuration;
        }
    }
}