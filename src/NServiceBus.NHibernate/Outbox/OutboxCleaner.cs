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
                frequencyToRunDeduplicationDataCleanup = TimeSpan.FromMinutes(1);
            }
            else if (!TimeSpan.TryParse(configValue, out frequencyToRunDeduplicationDataCleanup))
            {
                throw new Exception("Invalid value in \"NServiceBus/Outbox/NHibernate/FrequencyToRunDeduplicationDataCleanup\" AppSetting. Please ensure it is a TimeSpan.");
            }

            var key = "NServiceBus/Outbox/NHibernate/TimeToWaitBeforeTriggeringCriticalErrorWhenCleanupTaskFails";

            configValue = ConfigurationManager.AppSettings.Get(key);

            if (configValue == null)
            {
                timeToWaitBeforeTriggeringCriticalError = TimeSpan.FromMinutes(2);
            }
            else if (!TimeSpan.TryParse(configValue, out timeToWaitBeforeTriggeringCriticalError))
            {
                throw new Exception($"Invalid value in \"{key}\" AppSetting. Please ensure it is a TimeSpan.");
            }

            circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker(
                "OutboxCleanupTaskConnectivity",
                timeToWaitBeforeTriggeringCriticalError,
                ex => criticalError.Raise("Repeated failures when purging expired outbox records from storage, endpoint will be terminated.", ex)
                );

            cleanupTimer = new Timer(PerformCleanup, null, TimeSpan.FromMinutes(1), frequencyToRunDeduplicationDataCleanup);

            return Task.FromResult(true);
        }

        protected override Task OnStop(IMessageSession busSession)
        {
            using (var waitHandle = new ManualResetEvent(false))
            {
                cleanupTimer.Dispose(waitHandle);

                waitHandle.WaitOne();
            }

            return Task.FromResult(true);
        }

        void PerformCleanup(object state)
        {
            try
            {
                outboxPersister.RemoveEntriesOlderThan(DateTime.UtcNow - timeToKeepDeduplicationData);
                circuitBreaker.Success();
            }
            catch (Exception ex)
            {
                circuitBreaker.Failure(ex);
            }
        }

        // ReSharper disable NotAccessedField.Local
        Timer cleanupTimer;
        // ReSharper restore NotAccessedField.Local
        TimeSpan frequencyToRunDeduplicationDataCleanup;
        INHibernateOutboxStorage outboxPersister;
        TimeSpan timeToKeepDeduplicationData;
        RepeatedFailuresOverTimeCircuitBreaker circuitBreaker;
        TimeSpan timeToWaitBeforeTriggeringCriticalError;
        CriticalError criticalError;
    }
}