namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using Persistence.NHibernate;
    using TimeoutPersisters.NHibernate;
    using TimeoutPersisters.NHibernate.Config;
    using TimeoutPersisters.NHibernate.Installer;

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
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var builder = new NHibernateConfigurationBuilder(context.Settings, "Timeout", "NHibernate.Timeouts.Configuration", "StorageConfiguration");
            builder.AddMappings<TimeoutEntityMap>();
            var config = builder.Build();

            Func<string,Task> installAction = _ => Task.FromResult(0);

            if (RunInstaller(context))
            {
                installAction = identity =>
                {
                    new OptimizedSchemaUpdate(config.Configuration).Execute(false, true);
                    return Task.FromResult(0);
                };
            }

            context.Container.ConfigureComponent(b => new Installer.SchemaUpdater(installAction), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b =>
            {
                var sessionFactory = config.Configuration.BuildSessionFactory();
                return new TimeoutPersister(
                    context.Settings.EndpointName().ToString(),
                    sessionFactory,
                    new NHibernateSynchronizedStorageAdapter(sessionFactory), new NHibernateSynchronizedStorage(sessionFactory));
            }, DependencyLifecycle.SingleInstance);
        }

        static bool RunInstaller(FeatureConfigurationContext context)
        {
            return context.Settings.Get<bool>(context.Settings.HasSetting("NHibernate.Timeouts.AutoUpdateSchema")
                ? "NHibernate.Timeouts.AutoUpdateSchema"
                : "NHibernate.Common.AutoUpdateSchema");
        }
    }
}