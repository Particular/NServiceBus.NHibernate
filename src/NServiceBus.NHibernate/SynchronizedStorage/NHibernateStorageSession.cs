namespace NServiceBus.Features
{
    using System;
    using System.Configuration;
    using System.Dynamic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using global::NHibernate.Mapping.ByCode;
    using NHibernate.Outbox;
    using global::NHibernate.Transaction;
    using NServiceBus.Outbox.NHibernate;
    using TimeoutPersisters.NHibernate.Installer;
    using Persistence.NHibernate;
    using Configuration = global::NHibernate.Cfg.Configuration;
    using Environment = global::NHibernate.Cfg.Environment;
    using Installer = Persistence.NHibernate.Installer.Installer;

    /// <summary>
    /// NHibernate Storage Session.
    /// </summary>
    public class NHibernateStorageSession : Feature
    {
        internal const string OutboxMappingSettingsKey = "NServiceBus.NHibernate.OutboxMapping";
        internal const string OutboxConcurrencyModeSettingsKey = "NServiceBus.NHibernate.OutboxPessimisticMode";
        internal const string OutboxTransactionModeSettingsKey = "NServiceBus.NHibernate.OutboxTransactionScopeMode";

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
            dynamic diagnostics = new ExpandoObject();

            var builder = new NHibernateConfigurationBuilder(context.Settings, diagnostics, "Saga", "StorageConfiguration");
            var config = builder.Build();
            var sharedMappings = context.Settings.Get<SharedMappings>();

            var outboxEnabled = context.Settings.IsFeatureActive(typeof(Outbox));
            if (outboxEnabled)
            {
                sharedMappings.AddMapping(configuration => ApplyMappings(configuration, context));
                config.Configuration.Properties[Environment.TransactionStrategy] = typeof(AdoNetTransactionFactory).FullName;
            }

            sharedMappings.ApplyTo(config.Configuration);

            var sessionFactory = config.Configuration.BuildSessionFactory();

            context.Container.ConfigureComponent(b => new NHibernateSynchronizedStorage(sessionFactory), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => new NHibernateSynchronizedStorageAdapter(sessionFactory), DependencyLifecycle.SingleInstance);

            if (outboxEnabled)
            {
                var persisterFactory = context.Settings.Get<IOutboxPersisterFactory>();
                var pessimisticMode = context.Settings.GetOrDefault<bool>(OutboxConcurrencyModeSettingsKey);
                var transactionScopeMode = context.Settings.GetOrDefault<bool>(OutboxTransactionModeSettingsKey);
                var persister = persisterFactory.Create(sessionFactory, context.Settings.EndpointName(), pessimisticMode, transactionScopeMode);
                context.Container.ConfigureComponent(b => persister, DependencyLifecycle.SingleInstance);

                var timeToKeepDeduplicationData = GetTimeToKeepDeduplicationData();
                var deduplicationDataCleanupPeriod = GetDeduplicationDataCleanupPeriod();
                var outboxCleanupCriticalErrorTriggerTime = GetOutboxCleanupCriticalErrorTriggerTime();

                context.RegisterStartupTask(b => new OutboxCleaner(persister, b.Build<CriticalError>(), timeToKeepDeduplicationData, deduplicationDataCleanupPeriod, outboxCleanupCriticalErrorTriggerTime));

                context.Settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.NHibernate.Outbox", new
                {
                    TimeToKeepDeduplicationData = timeToKeepDeduplicationData,
                    DeduplicationDataCleanupPeriod = deduplicationDataCleanupPeriod,
                    OutboxCleanupCriticalErrorTriggerTime = outboxCleanupCriticalErrorTriggerTime,
                    RecordType = context.Settings.Get<Type>(OutboxMappingSettingsKey).FullName
                });
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


            context.Settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.NHibernate.SynchronizedSession", (object)diagnostics);
        }

        void ApplyMappings(Configuration config, FeatureConfigurationContext context)
        {
            var mapper = new ModelMapper();
            mapper.AddMapping(context.Settings.Get<Type>(OutboxMappingSettingsKey));
            config.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
        }

        static TimeSpan GetOutboxCleanupCriticalErrorTriggerTime()
        {
            var configValue = ConfigurationManager.AppSettings.Get("NServiceBus/Outbox/NHibernate/TimeToWaitBeforeTriggeringCriticalErrorWhenCleanupTaskFails");
            if (configValue == null)
            {
                return TimeSpan.FromMinutes(2);
            }
            if (TimeSpan.TryParse(configValue, out var typedValue))
            {
                return typedValue;
            }
            throw new Exception("Invalid value in \"NServiceBus/Outbox/NHibernate/TimeToWaitBeforeTriggeringCriticalErrorWhenCleanupTaskFails\" AppSetting. Please ensure it is a TimeSpan.");

        }

        static TimeSpan GetDeduplicationDataCleanupPeriod()
        {
            var configValue = ConfigurationManager.AppSettings.Get("NServiceBus/Outbox/NHibernate/FrequencyToRunDeduplicationDataCleanup");
            if (configValue == null)
            {
                return TimeSpan.FromMinutes(1);
            }
            if (TimeSpan.TryParse(configValue, out var typedValue))
            {
                return typedValue;
            }
            throw new Exception("Invalid value in \"NServiceBus/Outbox/NHibernate/FrequencyToRunDeduplicationDataCleanup\" AppSetting. Please ensure it is a TimeSpan.");

        }

        static TimeSpan GetTimeToKeepDeduplicationData()
        {
            var configValue = ConfigurationManager.AppSettings.Get("NServiceBus/Outbox/NHibernate/TimeToKeepDeduplicationData");
            if (configValue == null)
            {
                return TimeSpan.FromDays(7);
            }
            if (TimeSpan.TryParse(configValue, out var typedValue))
            {
                return typedValue;
            }
            throw new Exception("Invalid value in \"NServiceBus/Outbox/NHibernate/TimeToKeepDeduplicationData\" AppSetting. Please ensure it is a TimeSpan.");
        }
    }
}
