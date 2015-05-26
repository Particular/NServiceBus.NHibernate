namespace NServiceBus.Features
{
    using System;
    using NHibernate;
    using NHibernate.Cfg;
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
                configuration = new Configuration().SetProperties(properties);
            }
            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(configuration.Properties);

            context.Settings.Get<SharedMappings>().ApplyTo(configuration);

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

            var disableConnectionSharing = DisableTransportConnectionSharing(context);

            context.Container
                .ConfigureProperty<DbConnectionProvider>(p => p.DisableConnectionSharing, disableConnectionSharing)
                .ConfigureProperty<DbConnectionProvider>(p => p.DefaultConnectionString, connString);

            context.Pipeline.Register<OpenSqlConnectionBehavior.Registration>();
            context.Pipeline.Register<OpenSessionBehavior.Registration>();

            context.Container.ConfigureComponent<OpenSqlConnectionBehavior>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ConnectionString, connString)
                .ConfigureProperty(p => p.DisableConnectionSharing, disableConnectionSharing);

            context.Container.ConfigureComponent<OpenSessionBehavior>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ConnectionString, connString)
                .ConfigureProperty(p => p.DisableConnectionSharing, disableConnectionSharing)
                .ConfigureProperty(p => p.SessionCreator, context.Settings.GetOrDefault<Func<ISessionFactory, string, ISession>>("NHibernate.SessionCreator"));

            context.Container.ConfigureComponent(b => new NHibernateStorageContext(b.Build<PipelineExecutor>(), connString), DependencyLifecycle.InstancePerUnitOfWork);
            context.Container.ConfigureComponent<SharedConnectionStorageSessionProvider>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.ConnectionString, connString);

            if (context.Settings.GetOrDefault<bool>("NHibernate.RegisterManagedSession"))
            {
                context.Container.ConfigureComponent(b => b.Build<NHibernateStorageContext>().Session, DependencyLifecycle.InstancePerCall);
            }

            context.Container.ConfigureComponent<Installer>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.Configuration, configuration)
                .ConfigureProperty(x => x.RunInstaller, context.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema"));
        }

        static bool DisableTransportConnectionSharing(FeatureConfigurationContext context)
        {
            var nativeTransactions = context.Settings.GetOrDefault<bool>("Transactions.SuppressDistributedTransactions");
            return nativeTransactions;
        }
    }
}