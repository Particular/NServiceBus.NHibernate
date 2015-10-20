namespace NServiceBus.Features
{
    using System;
    using System.Configuration;
    using System.Threading;
    using NHibernate.Mapping.ByCode;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using Configuration = NHibernate.Cfg.Configuration;

    /// <summary>
    /// NHibernate Outbox Storage.
    /// </summary>
    public class NHibernateOutboxStorage : Feature
    {
        /// <summary>
        /// Creates an instance of <see cref="NHibernateOutboxStorage"/>.
        /// </summary>
        public NHibernateOutboxStorage()
        {
            DependsOn<Outbox>();
            RegisterStartupTask<OutboxCleaner>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Settings.Get<SharedMappings>()
                .AddMapping(ApplyMappings);

            context.Container.ConfigureComponent<OutboxPersister>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(op => op.EndpointName, context.Settings.EndpointName());
        }

        void ApplyMappings(Configuration config)
        {
            var mapper = new ModelMapper();
            mapper.AddMapping<OutboxEntityMap>();

            config.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
        }

        class OutboxCleaner:FeatureStartupTask
        {
            public OutboxCleaner(OutboxPersister outboxPersister, CriticalError criticalError)
            {
                this.outboxPersister = outboxPersister;
                this.criticalError = criticalError;
            }
 
            protected override void OnStart()
            {
                var configValue = ConfigurationManager.AppSettings.Get("NServiceBus/Outbox/NHibernate/TimeToKeepDeduplicationData");

                if (configValue == null)
                {
                    timeToKeepDeduplicationData = TimeSpan.FromDays(7);
                }
                else
                {
                    if (!TimeSpan.TryParse(configValue, out timeToKeepDeduplicationData))
                    {
                        throw new Exception("Invalid value in \"NServiceBus/Outbox/NHibernate/TimeToKeepDeduplicationData\" AppSetting. Please ensure it is a TimeSpan.");
                    }
                }

                configValue = ConfigurationManager.AppSettings.Get("NServiceBus/Outbox/NHibernate/FrequencyToRunDeduplicationDataCleanup");

                if (configValue == null)
                {
                    frequencyToRunDeduplicationDataCleanup = TimeSpan.FromMinutes(1);
                }
                else
                {
                    if (!TimeSpan.TryParse(configValue, out frequencyToRunDeduplicationDataCleanup))
                    {
                        throw new Exception("Invalid value in \"NServiceBus/Outbox/NHibernate/FrequencyToRunDeduplicationDataCleanup\" AppSetting. Please ensure it is a TimeSpan.");
                    }
                }

                cleanupTimer = new Timer(PerformCleanup, null, TimeSpan.FromMinutes(1), frequencyToRunDeduplicationDataCleanup);
            }

            protected override void OnStop()
            {
                using (var waitHandle = new ManualResetEvent(false))
                {
                    cleanupTimer.Dispose(waitHandle);

                    waitHandle.WaitOne();
                }
            }

            void PerformCleanup(object state)
            {
                try
                {
                    outboxPersister.RemoveEntriesOlderThan(DateTime.UtcNow - timeToKeepDeduplicationData);
                    cleanupFailures = 0;
                }
                catch (Exception ex)
                {
                    cleanupFailures++;
                    if (cleanupFailures >= 10)
                    {
                        criticalError.Raise("Failed to clean expired Outbox records after 10 consecutive unsuccessful attempts. The most likely cause of this is connectivity issues with your database.", ex);
                        throw;
                    }
                }
            }
 
// ReSharper disable NotAccessedField.Local
            Timer cleanupTimer;
// ReSharper restore NotAccessedField.Local
            OutboxPersister outboxPersister;
            CriticalError criticalError;
            int cleanupFailures;
            TimeSpan timeToKeepDeduplicationData;
            TimeSpan frequencyToRunDeduplicationDataCleanup;
        }
    }
}