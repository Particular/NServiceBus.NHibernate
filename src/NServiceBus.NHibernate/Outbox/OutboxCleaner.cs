namespace NServiceBus.Features
{
    using System;
    using System.Configuration;
    using System.Threading;
    using System.Threading.Tasks;

    class OutboxCleaner : FeatureStartupTask
    {
        public OutboxCleaner(INHibernateOutboxStorage outboxPersister, CriticalError criticalError)
        {
            this.outboxPersister = outboxPersister;
            this.criticalError = criticalError;
        }

        protected override Task OnStart(IMessageSession busSession)
        {
            var configValue = ConfigurationManager.AppSettings.Get("NServiceBus/Outbox/NHibernate/TimeToKeepDeduplicationData");

            if (configValue == null)
            {
                timeToKeepDeduplicationData = TimeSpan.FromDays(7);
            }
            else if (!TimeSpan.TryParse(configValue, out timeToKeepDeduplicationData))
            {
                throw new Exception("Invalid value in \"NServiceBus/Outbox/NHibernate/TimeToKeepDeduplicationData\" AppSetting. Please ensure it is a TimeSpan.");
            }

            configValue = ConfigurationManager.AppSettings.Get("NServiceBus/Outbox/NHibernate/FrequencyToRunDeduplicationDataCleanup");

            if (configValue == null)
            {
                frequencyToRunDeduplicationDataCleanup = DefaultFrequencyToRunDeduplicationDataCleanup;
            }
            else if (!TimeSpan.TryParse(configValue, out frequencyToRunDeduplicationDataCleanup))
            {
                throw new Exception("Invalid value in \"NServiceBus/Outbox/NHibernate/FrequencyToRunDeduplicationDataCleanup\" AppSetting. Please ensure it is a TimeSpan.");
            }

            configValue = ConfigurationManager.AppSettings.Get("NServiceBus/Outbox/NHibernate/TimeToWaitBeforeTriggeringCriticalErrorWhenCleanupTaskFails");

            if (configValue == null)
            {
                timeToWaitBeforeTriggeringCriticalError = DefaultTimeToWaitBeforeTriggeringCriticalError;
            }
            else if (!TimeSpan.TryParse(configValue, out timeToWaitBeforeTriggeringCriticalError))
            {
                throw new Exception("Invalid value in \"NServiceBus/Outbox/NHibernate/TimeToWaitBeforeTriggeringCriticalErrorWhenCleanupTaskFails\" AppSetting. Please ensure it is a TimeSpan.");
            }

            circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker(
                "OutboxCleanupTaskConnectivity",
                timeToWaitBeforeTriggeringCriticalError,
                ex => criticalError.Raise("Failed to clean the Oubox.", ex)
            );


            cleanupTimer = new AsyncTimer();
            cleanupTimer.Start(PerformCleanup, frequencyToRunDeduplicationDataCleanup, ex => circuitBreaker.Failure(ex));

            return Task.FromResult(true);
        }

        protected override Task OnStop(IMessageSession busSession)
        {
            return cleanupTimer.Stop();
        }

        async Task PerformCleanup()
        {
            await Task.Run(() => outboxPersister.RemoveEntriesOlderThan(DateTime.UtcNow - timeToKeepDeduplicationData)).ConfigureAwait(false);
            circuitBreaker.Success();
        }

        RepeatedFailuresOverTimeCircuitBreaker circuitBreaker;

        // ReSharper disable NotAccessedField.Local
        AsyncTimer cleanupTimer;
        // ReSharper restore NotAccessedField.Local
        TimeSpan timeToKeepDeduplicationData;
        CriticalError criticalError;
        TimeSpan frequencyToRunDeduplicationDataCleanup;
        INHibernateOutboxStorage outboxPersister;
        TimeSpan timeToWaitBeforeTriggeringCriticalError;

        static readonly TimeSpan DefaultFrequencyToRunDeduplicationDataCleanup = TimeSpan.FromMinutes(1);
        static readonly TimeSpan DefaultTimeToWaitBeforeTriggeringCriticalError = TimeSpan.FromMinutes(2);
        static readonly TimeSpan OutboxCleanupStartupDelay = TimeSpan.FromMinutes(1);
    }
}