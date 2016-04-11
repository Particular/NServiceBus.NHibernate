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

    class NHibernateSynchronizedStorageFeature : Feature
    {
        internal NHibernateSynchronizedStorageFeature()
        {
            Defaults(s => s.SetDefault<SharedMappings>(new SharedMappings()));
            DependsOnOptionally<Outbox>();
        }

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

            if (outboxEnabled)
            {
                context.Container.ConfigureComponent(b => new OutboxPersister(sessionFactory, context.Settings.EndpointName().ToString()), DependencyLifecycle.SingleInstance);
                context.RegisterStartupTask(b => new OutboxCleaner(b.Build<OutboxPersister>()));
            }

            var runInstaller = context.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema");

            Func<string, Task> installAction = _ => Task.FromResult(0);

            if (runInstaller)
            {
                installAction = identity =>
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


            context.Container.ConfigureComponent(b => new Installer.SchemaUpdater(installAction), DependencyLifecycle.SingleInstance);
        }

        void ApplyMappings(Configuration config)
        {
            var mapper = new ModelMapper();
            mapper.AddMapping<OutboxEntityMap>();

            config.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
        }
    }
}