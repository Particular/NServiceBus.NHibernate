namespace NServiceBus.Features
{
    using System;
    using System.Configuration;
    using System.Transactions;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Transaction;
    using Microsoft.Extensions.DependencyInjection;
    using NHibernate.Outbox;
    using NHibernate.SynchronizedStorage;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using Persistence.NHibernate;
    using Configuration = global::NHibernate.Cfg.Configuration;
    using Environment = global::NHibernate.Cfg.Environment;

    internal sealed class NHibernateOutbox : Feature
    {

        internal const string OutboxMappingSettingsKey = "NServiceBus.NHibernate.OutboxMapping";
        internal const string OutboxTableNameSettingsKey = "NServiceBus.NHibernate.OutboxTableName";
        internal const string OutboxSchemaNameSettingsKey = "NServiceBus.NHibernate.OutboxSchemaName";
        internal const string OutboxConcurrencyModeSettingsKey = "NServiceBus.NHibernate.OutboxPessimisticMode";
        internal const string OutboxTransactionModeSettingsKey = "NServiceBus.NHibernate.OutboxTransactionScopeMode";
        internal const string OutboxTransactionIsolationLevelSettingsKey = "NServiceBus.NHibernate.OutboxTransactionIsolationLevel";
        internal const string OutboxTransactionScopeModeIsolationLevelSettingsKey = "NServiceBus.NHibernate.OutboxTransactionScopeModeIsolationLevel";
        internal const string ProcessorEndpointKey = "NHibernate.TransactionalSession.ProcessorEndpoint";
        internal const string DisableOutboxTableCreationSettingKey = "NServiceBus.NHibernate.DisableOutboxTableCreation";

        internal NHibernateOutbox()
        {
            Enable<NHibernateStorageSession>();

            DependsOn<Outbox>();
            DependsOn<NHibernateStorageSession>();
        }

        /// <inheritdoc />
        protected override void Setup(FeatureConfigurationContext context)
        {
            var config = context.Settings.Get<NHibernateConfiguration>();

            var pessimisticMode = context.Settings.GetOrDefault<bool>(OutboxConcurrencyModeSettingsKey);
            var transactionScopeMode = context.Settings.GetOrDefault<bool>(OutboxTransactionModeSettingsKey);
            var transactionScopeIsolationLevel = context.Settings.GetOrDefault<IsolationLevel>(OutboxTransactionScopeModeIsolationLevelSettingsKey);
            var adoIsolationLevel = context.Settings.GetOrDefault<System.Data.IsolationLevel>(OutboxTransactionIsolationLevelSettingsKey);
            if (adoIsolationLevel == default)
            {
                //Default to Read Committed
                adoIsolationLevel = System.Data.IsolationLevel.ReadCommitted;
            }

            config.Configuration.Properties[Environment.TransactionStrategy] = typeof(AdoNetTransactionFactory).FullName; //Can we move it to defaukt

            var configuredOutboxRecordType = context.Settings.GetOrDefault<Type>(OutboxMappingSettingsKey);
            var actualOutboxRecordType = configuredOutboxRecordType ?? typeof(OutboxRecordMapping);
            var outboxTableName = context.Settings.GetOrDefault<string>(OutboxTableNameSettingsKey);
            var outboxSchemaName = context.Settings.GetOrDefault<string>(OutboxSchemaNameSettingsKey);

            var timeToKeepDeduplicationData = GetTimeToKeepDeduplicationData();
            var deduplicationDataCleanupPeriod = GetDeduplicationDataCleanupPeriod();
            var outboxCleanupCriticalErrorTriggerTime = GetOutboxCleanupCriticalErrorTriggerTime();

            if (outboxTableName != null && configuredOutboxRecordType != null)
            {
                throw new Exception("Custom outbox table name and custom outbox record type cannot be specified at the same time.");
            }

            var disableOutboxTableCreation = context.Settings.GetOrDefault<bool>(DisableOutboxTableCreationSettingKey);

            ApplyMappings(config.Configuration, actualOutboxRecordType, outboxTableName, outboxSchemaName, disableOutboxTableCreation);

            var persisterFactory = context.Settings.Get<IOutboxPersisterFactory>();
            context.Services.AddSingleton<IOutboxStorage>(sp =>
            {
                var holder = sp.GetRequiredService<SessionFactoryHolder>();
                var endpointName = context.Settings.GetOrDefault<string>(ProcessorEndpointKey) ?? context.Settings.EndpointName();
                var persister = persisterFactory.Create(holder.SessionFactory, endpointName, pessimisticMode, transactionScopeMode, adoIsolationLevel, transactionScopeIsolationLevel);
                return persister;
            });

            context.RegisterStartupTask(b => new OutboxCleaner((INHibernateOutboxStorage)b.GetRequiredService<IOutboxStorage>(), b.GetRequiredService<CriticalError>(), timeToKeepDeduplicationData, deduplicationDataCleanupPeriod, outboxCleanupCriticalErrorTriggerTime));

            context.Settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.NHibernate.Outbox", new
            {
                TimeToKeepDeduplicationData = timeToKeepDeduplicationData,
                DeduplicationDataCleanupPeriod = deduplicationDataCleanupPeriod,
                OutboxCleanupCriticalErrorTriggerTime = outboxCleanupCriticalErrorTriggerTime,
                RecordType = actualOutboxRecordType.FullName,
                CustomOutboxTableName = outboxTableName,
                CustomOutboxSchemaName = outboxSchemaName,
                OutboxTableCreationDisabled = disableOutboxTableCreation
            });
        }

        static void ApplyMappings(Configuration config, Type outboxRecordType, string customOutboxTableName, string customOutboxSchemaName, bool disableSchemaExport = false)
        {
            var mapper = new ModelMapper();
            mapper.BeforeMapClass += (inspector, type, customizer) =>
            {
                if (customOutboxTableName != null)
                {
                    customizer.Table(customOutboxTableName);
                }
                if (customOutboxSchemaName != null)
                {
                    customizer.Schema(customOutboxSchemaName);
                }

                // Set schema-export attribute to none if schema export is disabled
                if (disableSchemaExport)
                {
                    customizer.SchemaAction(SchemaAction.None);
                }
            };
            mapper.AddMapping(outboxRecordType);
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