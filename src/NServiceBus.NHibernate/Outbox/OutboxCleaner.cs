namespace NServiceBus.Features
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Persistence.NHibernate;

    class OutboxCleaner : FeatureStartupTask
    {
        public OutboxPersister OutboxPersister { get; }

        public OutboxCleaner(OutboxPersister outboxPersister, TimeSpan timeToKeepDeduplicationData, TimeSpan frequencyToRunDeduplicationDataCleanup)
        {
            OutboxPersister = outboxPersister;
            this.timeToKeepDeduplicationData = timeToKeepDeduplicationData;
            this.frequencyToRunDeduplicationDataCleanup = frequencyToRunDeduplicationDataCleanup;
        }

        protected override Task OnStart(IMessageSession busSession)
        {
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
            OutboxPersister.RemoveEntriesOlderThan(DateTime.UtcNow - timeToKeepDeduplicationData);
        }

        // ReSharper disable NotAccessedField.Local
        Timer cleanupTimer;
        // ReSharper restore NotAccessedField.Local
        TimeSpan timeToKeepDeduplicationData;
        TimeSpan frequencyToRunDeduplicationDataCleanup;
    }
}