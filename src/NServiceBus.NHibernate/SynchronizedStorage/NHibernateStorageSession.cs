namespace NServiceBus.Features
{
    using System.Transactions;
    using System;
    using System.Configuration;
    using System.Dynamic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using NServiceBus.NHibernate.Outbox;
    using global::NHibernate.Transaction;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.Persistence;
    using NServiceBus.Persistence.NHibernate;
    using global::NHibernate.Tool.hbm2ddl;
    using Configuration = global::NHibernate.Cfg.Configuration;
    using Environment = global::NHibernate.Cfg.Environment;
    using Installer = Persistence.NHibernate.Installer.Installer;

    class SessionFactoryHolder
    {
        public SessionFactoryHolder(ISessionFactory sessionFactory)
        {
            SessionFactory = sessionFactory;
        }

        public ISessionFactory SessionFactory { get; }
    }

    /// <summary>
    /// NHibernate Outbox feature
    /// </summary>
    public class NHibernateOutbox : Feature
    {

        internal const string OutboxMappingSettingsKey = "NServiceBus.NHibernate.OutboxMapping";
        internal const string OutboxTableNameSettingsKey = "NServiceBus.NHibernate.OutboxTableName";
        internal const string OutboxSchemaNameSettingsKey = "NServiceBus.NHibernate.OutboxSchemaName";
        internal const string OutboxConcurrencyModeSettingsKey = "NServiceBus.NHibernate.OutboxPessimisticMode";
        internal const string OutboxTransactionModeSettingsKey = "NServiceBus.NHibernate.OutboxTransactionScopeMode";
        internal const string OutboxTransactionIsolationLevelSettingsKey = "NServiceBus.NHibernate.OutboxTransactionIsolationLevel";
        internal const string OutboxTransactionScopeModeIsolationLevelSettingsKey = "NServiceBus.NHibernate.OutboxTransactionScopeModeIsolationLevel";

        /// <summary>
        /// Creates a new instance of the feature
        /// </summary>
        public NHibernateOutbox()
        {
            Defaults(x => x.EnableFeatureByDefault<NHibernateStorageSession>());

            DependsOn<Outbox>();
            DependsOn<NHibernateStorageSession>();
        }

        /// <inheritdoc />
        protected override void Setup(FeatureConfigurationContext context)
        {
            var config = context.Settings.Get<NHibernateConfiguration>(); //TODO: Should we register it under a Seaga key?

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

            ApplyMappings(config.Configuration, actualOutboxRecordType, outboxTableName, outboxSchemaName);

            var persisterFactory = context.Settings.Get<IOutboxPersisterFactory>();
            context.Services.AddSingleton<IOutboxStorage>(sp =>
            {
                var holder = sp.GetRequiredService<SessionFactoryHolder>();
                var persister = persisterFactory.Create(holder.SessionFactory, context.Settings.EndpointName(), pessimisticMode, transactionScopeMode, adoIsolationLevel, transactionScopeIsolationLevel);
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
                CustomOutboxSchemaName = outboxSchemaName
            });
        }

        static void ApplyMappings(Configuration config, Type outboxRecordType, string customOutboxTableName, string customOutboxSchemaName)
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


    /// <summary>
    /// NHibernate Storage Session.
    /// </summary>
    public class NHibernateStorageSession : Feature
    {

        internal NHibernateStorageSession()
        {

            Defaults(s =>
            {
                var diagnosticsObject = new ExpandoObject();
                var builder = new NHibernateConfigurationBuilder(s, diagnosticsObject, "Saga", "StorageConfiguration");
                var config = builder.Build();
                s.SetDefault(config);
                s.SetDefault("NServiceBus.NHibernate.NHibernateStorageSessionDiagnostics", diagnosticsObject);

                s.SetDefault<IOutboxPersisterFactory>(new OutboxPersisterFactory<OutboxRecord>());
            });

            // since the installers are registered even if the feature isn't enabled we need to make
            // this a no-op of there is no "schema updater" available
            Defaults(c => c.Set(new Installer.SchemaUpdater()));
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var config = context.Settings.Get<NHibernateConfiguration>(); //TODO: Should we register it under a Seaga key?

            var sessionHolder = new CurrentSessionHolder();

            context.Services.AddTransient(_ => sessionHolder.Current);
            context.Pipeline.Register(new CurrentSessionBehavior(sessionHolder), "Manages the lifecycle of the current session holder.");

            context.Services.AddSingleton(sb =>
            {
                //Outbox and Sagas need to run before this line to include their mappings
                var sessionFactory = config.Configuration.BuildSessionFactory();
                return new SessionFactoryHolder(sessionFactory);
            });

            context.Services.AddSingleton<ISynchronizedStorage>(sb => new NHibernateSynchronizedStorage(sb.GetRequiredService<SessionFactoryHolder>().SessionFactory, sessionHolder));
            context.Services.AddSingleton<ISynchronizedStorageAdapter>(sb => new NHibernateSynchronizedStorageAdapter(sb.GetRequiredService<SessionFactoryHolder>().SessionFactory, sessionHolder));

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


            context.Settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.NHibernate.SynchronizedSession", context.Settings.Get("NServiceBus.NHibernate.NHibernateStorageSessionDiagnostics"));
        }


    }
}
