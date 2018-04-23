namespace NServiceBus.Features
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using global::NHibernate.Cfg;
    using global::NHibernate.Mapping.ByCode;
    using NHibernate.Outbox;
    using global::NHibernate.Transaction;
    using NServiceBus.Outbox.NHibernate;
    using TimeoutPersisters.NHibernate.Installer;
    using Persistence.NHibernate;
    using Environment = global::NHibernate.Cfg.Environment;
    using Installer = Persistence.NHibernate.Installer.Installer;

    /// <summary>
    /// NHibernate Storage Session.
    /// </summary>
    public class NHibernateStorageSession : Feature
    {
        internal const string OutboxMappingSettingsKey = "NServiceBus.NHibernate.OutboxMapping";
        internal const string OutboxTableNameSettingsKey = "NServiceBus.NHibernate.OutboxTableName";
        internal const string OutboxSchemaNameSettingsKey = "NServiceBus.NHibernate.OutboxSchemaName";
        internal const string OutboxTimeToKeepDeduplicationDataSettingsKey = "NServiceBus.NHibernate.TimeToKeepDeduplicationData";
        internal const string OutboxCleanupIntervalSettingsKey = "NServiceBus.NHibernate.FrequencyToRunDeduplicationDataCleanup";

        internal NHibernateStorageSession()
        {
            DependsOnOptionally<Outbox>();

            Defaults(s =>
            {
                s.SetDefault<SharedMappings>(new SharedMappings());
                s.SetDefault<IOutboxPersisterFactory>(new OutboxPersisterFactory<OutboxRecord>());
                s.SetDefault(OutboxMappingSettingsKey, typeof(OutboxRecordMapping));
            });

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
                sharedMappings.AddMapping(configuration => ApplyMappings(configuration, context));
                config.Configuration.Properties[Environment.TransactionStrategy] = typeof(AdoNetTransactionFactory).FullName;

                string tableName;
                if (context.Settings.TryGet(OutboxTableNameSettingsKey, out tableName))
                {
                    outboxTableName = tableName;
                }

                string schemaName;
                if (context.Settings.TryGet(OutboxSchemaNameSettingsKey, out schemaName))
                {
                    outboxSchemaName = schemaName;
                }
            }

            sharedMappings.ApplyTo(config.Configuration);

            var sessionFactory = config.Configuration.BuildSessionFactory();

            context.Container.ConfigureComponent(b => new NHibernateSynchronizedStorage(sessionFactory), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => new NHibernateSynchronizedStorageAdapter(sessionFactory), DependencyLifecycle.SingleInstance);
            //Legacy
            context.Container.ConfigureComponent(b => new NHibernateStorageContext(), DependencyLifecycle.InstancePerUnitOfWork);

            if (outboxEnabled)
            {
                var factory = context.Settings.Get<IOutboxPersisterFactory>();
                var persister = factory.Create(sessionFactory, context.Settings.EndpointName());
                context.Container.ConfigureComponent(b => persister, DependencyLifecycle.SingleInstance);
                context.RegisterStartupTask(b => new OutboxCleaner(persister, b.Build<CriticalError>()));
            }

            var runInstaller = context.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema");

            if (runInstaller)
            {
                context.Settings.Get<Installer.SchemaUpdater>().Execute = identity =>
                {
                    var schemaUpdate = new OptimizedSchemaUpdate(config.Configuration);
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

        void ApplyMappings(Configuration config, FeatureConfigurationContext context)
        {
            var mapper = new ModelMapper();
            mapper.BeforeMapClass += OutboxTableAndSchemaOverride;
            mapper.AddMapping(context.Settings.Get<Type>(OutboxMappingSettingsKey));
            config.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
        }

        void OutboxTableAndSchemaOverride(IModelInspector modelInspector, Type type, IClassAttributesMapper map)
        {
            if (type != typeof(OutboxRecordMapping)) return;
            if (outboxTableName != null) map.Table(outboxTableName);
            if (outboxSchemaName != null) map.Schema(outboxSchemaName);
        }

        string outboxTableName;
        string outboxSchemaName;
    }
}
