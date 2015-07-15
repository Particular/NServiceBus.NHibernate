namespace NServiceBus.Features
{
    using System;
    using NHibernate;
    using Persistence.NHibernate;
    using Pipeline;

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
            var configure = new ConfigureNHibernate(context.Settings, "Saga", "StorageConfiguration");
            context.Settings.Get<SharedMappings>().ApplyTo(configure.Configuration);

            context.Container.RegisterSingleton(new SessionFactoryProvider(configure.Configuration.BuildSessionFactory()));

            var disableConnectionSharing = DisableTransportConnectionSharing(context);

            context.Container
                .ConfigureProperty<DbConnectionProvider>(p => p.DisableConnectionSharing, disableConnectionSharing)
                .ConfigureProperty<DbConnectionProvider>(p => p.DefaultConnectionString, configure.ConnectionString);

            context.Pipeline.Register<OpenSqlConnectionBehavior.Registration>();
            context.Pipeline.Register<OpenSessionBehavior.Registration>();

            context.Container.ConfigureComponent<OpenSqlConnectionBehavior>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ConnectionString, configure.ConnectionString)
                .ConfigureProperty(p => p.DisableConnectionSharing, disableConnectionSharing);

            context.Container.ConfigureComponent<OpenSessionBehavior>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ConnectionString, configure.ConnectionString)
                .ConfigureProperty(p => p.DisableConnectionSharing, disableConnectionSharing)
                .ConfigureProperty(p => p.SessionCreator, context.Settings.GetOrDefault<Func<ISessionFactory, string, ISession>>("NHibernate.SessionCreator"));

            context.Container.ConfigureComponent(b => new NHibernateStorageContext(b.Build<PipelineExecutor>(), configure.ConnectionString), DependencyLifecycle.InstancePerUnitOfWork);
            context.Container.ConfigureComponent<SharedConnectionStorageSessionProvider>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.ConnectionString, configure.ConnectionString);

            if (context.Settings.GetOrDefault<bool>("NHibernate.RegisterManagedSession"))
            {
                context.Container.ConfigureComponent(b => b.Build<NHibernateStorageContext>().Session, DependencyLifecycle.InstancePerCall);
            }

            context.Container.ConfigureComponent<Installer>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.Configuration, configure.Configuration)
                .ConfigureProperty(x => x.RunInstaller, context.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema"));
        }

        static bool DisableTransportConnectionSharing(FeatureConfigurationContext context)
        {
            var nativeTransactions = context.Settings.GetOrDefault<bool>("Transactions.SuppressDistributedTransactions");
            return nativeTransactions;
        }
    }
}