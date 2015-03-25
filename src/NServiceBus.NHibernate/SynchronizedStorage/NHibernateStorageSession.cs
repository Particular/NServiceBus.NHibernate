namespace NServiceBus.Features
{
    using NHibernate.Cfg;
    using NHibernate.Mapping.ByCode;
    using NServiceBus.Outbox.NHibernate;
    using Persistence.NHibernate;
    using Persistence.NHibernate.Installer;

    /// <summary>
    /// NHibernate Storage Session.
    /// </summary>
    public class NHibernateStorageSession : Feature
    {
        internal NHibernateStorageSession()
        {
            Defaults(s => s.SetDefault<SharedMappings>(new SharedMappings()));
            DependsOnOptionally<Outbox>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var builder = new NHibernateConfigurationBuilder(context.Settings, "Saga", "StorageConfiguration");
            var config = builder.Build();
            var sharedMappings = context.Settings.Get<SharedMappings>();

            var outboxEnabled = context.Settings.IsFeatureActive(typeof(Outbox));
            if (outboxEnabled)
            {
                sharedMappings.AddMapping(ApplyMappings);
            }

            sharedMappings.ApplyTo(config.Configuration);

            var sessionFactory = config.Configuration.BuildSessionFactory();

            context.Container.ConfigureComponent(b => new NHibernateSynchronizedStorage(sessionFactory), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => new NHibernateSynchronizedStorageAdapter(sessionFactory), DependencyLifecycle.SingleInstance);
            //Legacy
            context.Container.ConfigureComponent(b => new NHibernateStorageContext(), DependencyLifecycle.InstancePerUnitOfWork);

            if (outboxEnabled)
            {
                context.Container.ConfigureComponent(b => new OutboxPersister(sessionFactory, context.Settings.EndpointName().ToString()), DependencyLifecycle.SingleInstance);
                context.RegisterStartupTask(b => new OutboxCleaner(b.Build<OutboxPersister>()));
            }

            var runInstaller = context.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema");

            context.Container.ConfigureComponent<Installer>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.Configuration, runInstaller ? new Installer.ConfigWrapper(config.Configuration) : null);
        }

        void ApplyMappings(Configuration config)
        {
            var mapper = new ModelMapper();
            mapper.AddMapping<OutboxEntityMap>();

            config.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
        }
    }
}