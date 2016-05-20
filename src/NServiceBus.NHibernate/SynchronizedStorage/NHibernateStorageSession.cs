namespace NServiceBus.Features
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using global::NHibernate.Cfg;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
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
            DependsOnOptionally<Outbox>();

            Defaults(s => s.SetDefault<SharedMappings>(new SharedMappings()));

            // since the installers are registered even if the feature isn't enabled we need to make 
            // this a no-op of there is no "schema updater" available
            Defaults(c => c.Set<Installer.SchemaUpdater>(new Installer.SchemaUpdater()));
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

            if (runInstaller)
            {
                context.Settings.Get<Installer.SchemaUpdater>().Execute = identity =>
                {
                    var schemaUpdate = new SchemaUpdate(config.Configuration);
                    var sb = new StringBuilder();
                    schemaUpdate.Execute(s => sb.AppendLine(s), true);

                    if (schemaUpdate.Exceptions.Any())
                    {
                        var aggregate = new AggregateException(schemaUpdate.Exceptions);

                        var errorMessage = @"Schema update failed.
The following exception(s) were thrown:
{0}

TSql Script:
{1}";
                        throw new Exception(string.Format(errorMessage, aggregate.Flatten(), sb));
                    }



                    return Task.FromResult(0);
                };
            }
        }

        void ApplyMappings(Configuration config)
        {
            var mapper = new ModelMapper();
            mapper.AddMapping<OutboxEntityMap>();

            config.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
        }
    }
}