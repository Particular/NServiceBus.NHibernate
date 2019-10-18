namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate.Cfg;
    using Logging;
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
        /// Creates an instance of <see cref="NHibernateTimeoutStorage" />.
        /// </summary>
        public NHibernateTimeoutStorage()
        {
            DependsOn<TimeoutManager>();

            // since the installers are registered even if the feature isn't enabled we need to make 
            // this a no-op of there is no "schema updater" available
            Defaults(c => c.Set<Installer.SchemaUpdater>(new Installer.SchemaUpdater()));
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var builder = new NHibernateConfigurationBuilder(context.Settings, "Timeout", "NHibernate.Timeouts.Configuration", "StorageConfiguration");
            builder.AddMappings<TimeoutEntityMap>();
            var config = builder.Build();

            if (RunInstaller(context))
            {
                context.Settings.Get<Installer.SchemaUpdater>().Execute = identity =>
                {
                    new OptimizedSchemaUpdate(config.Configuration).Execute(false, true);
                    return Task.FromResult(0);
                };
            }

            var timeoutsCleanupExecutionInterval = context.Settings.GetOrDefault<TimeSpan?>("NHibernate.Timeouts.CleanupExecutionInterval") ?? TimeSpan.FromMinutes(2);

            context.Container.ConfigureComponent(b =>
            {
                var sessionFactory = config.Configuration.BuildSessionFactory();
                return new TimeoutPersister(
                    context.Settings.EndpointName(),
                    sessionFactory,
                    new NHibernateSynchronizedStorageAdapter(sessionFactory), new NHibernateSynchronizedStorage(sessionFactory),
                    timeoutsCleanupExecutionInterval);
            }, DependencyLifecycle.SingleInstance);

            context.RegisterStartupTask(new DetectIncorrectIndexesStartupTask(config.Configuration));
        }

        static bool RunInstaller(FeatureConfigurationContext context)
        {
            return context.Settings.Get<bool>(context.Settings.HasSetting("NHibernate.Timeouts.AutoUpdateSchema")
                ? "NHibernate.Timeouts.AutoUpdateSchema"
                : "NHibernate.Common.AutoUpdateSchema");
        }

        class DetectIncorrectIndexesStartupTask : FeatureStartupTask
        {
            static readonly ILog Logger = LogManager.GetLogger(typeof(DetectIncorrectIndexesStartupTask));

            public DetectIncorrectIndexesStartupTask(Configuration configuration)
            {
                this.configuration = configuration;
            }

            protected override Task OnStart(IMessageSession session)
            {
                var result = new TimeoutsIndexValidator(configuration).Validate();

                if (result.IsValid)
                {
                    return Task.FromResult(0);
                }

                if (result.Exception != null)
                {
                    Logger.Warn(result.ErrorDescription, result.Exception);
                }
                else
                {
                    Logger.Warn(result.ErrorDescription);
                }

                return Task.FromResult(0);
            }

            protected override Task OnStop(IMessageSession session)
            {
                return Task.FromResult(0);
            }

            Configuration configuration;
        }
    }
}