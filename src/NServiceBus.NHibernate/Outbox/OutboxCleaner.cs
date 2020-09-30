namespace NServiceBus.Features
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;

    class OutboxCleaner : FeatureStartupTask
    {
        public OutboxCleaner(INHibernateOutboxStorage outboxPersister, CriticalError criticalError, TimeSpan timeToKeepDeduplicationData, TimeSpan deduplicationDataCleanupPeriod, TimeSpan criticalErrorTriggerTime)
        {
            this.outboxPersister = outboxPersister;
            this.criticalError = criticalError;
            this.timeToKeepDeduplicationData = timeToKeepDeduplicationData;
            this.deduplicationDataCleanupPeriod = deduplicationDataCleanupPeriod;
            this.criticalErrorTriggerTime = criticalErrorTriggerTime;
        }

        protected override Task OnStart(IMessageSession busSession)
        {
            circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker(
                "OutboxCleanupTaskConnectivity",
                criticalErrorTriggerTime,
                ex => criticalError.Raise("Failed to clean the Outbox.", ex)
            );

            if (deduplicationDataCleanupPeriod == Timeout.InfiniteTimeSpan)
            {
                Logger.InfoFormat("Outbox cleanup task is disabled.");
            }

            cancellationTokenSource = new CancellationTokenSource();

            cleanup = Task.Run(() => PerformCleanup(cancellationTokenSource.Token), CancellationToken.None);

            return Task.CompletedTask;
        }
        

        protected override Task OnStop(IMessageSession busSession)
        {
            cancellationTokenSource.Cancel();
            return cleanup;
        }

        async Task PerformCleanup(CancellationToken ct)
        {
            while (ct.IsCancellationRequested == false)
            {
                try
                {
                    await Task.Delay(deduplicationDataCleanupPeriod, ct).ConfigureAwait(false);
                    await outboxPersister.RemoveEntriesOlderThan(DateTime.UtcNow - timeToKeepDeduplicationData).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // no-op
                }
                catch (Exception ex)
                {
                    circuitBreaker.Failure(ex);
                }
            }
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(OutboxCleaner));
        RepeatedFailuresOverTimeCircuitBreaker circuitBreaker;

        Task cleanup;
        CancellationTokenSource cancellationTokenSource;

        TimeSpan timeToKeepDeduplicationData;
        CriticalError criticalError;
        TimeSpan deduplicationDataCleanupPeriod;
        INHibernateOutboxStorage outboxPersister;
        TimeSpan criticalErrorTriggerTime;
    }
}