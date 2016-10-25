namespace NServiceBus.Features
{
    using System;
    using System.Configuration;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Persistence.NHibernate;

    class OutboxCleaner : FeatureStartupTask
    {
        public OutboxCleaner(INHibernateOutboxStorage outboxPersister)
        {
            this.outboxPersister = outboxPersister;
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
            outboxPersister.RemoveEntriesOlderThan(DateTime.UtcNow - timeToKeepDeduplicationData);
        }

        // ReSharper disable NotAccessedField.Local
        Timer cleanupTimer;
        // ReSharper restore NotAccessedField.Local
        TimeSpan frequencyToRunDeduplicationDataCleanup;
        INHibernateOutboxStorage outboxPersister;
        TimeSpan timeToKeepDeduplicationData;
    }
}