namespace NServiceBus.Features
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using NServiceBus;

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

        protected override Task OnStart(IMessageSession busSession, CancellationToken cancellationToken = default)
        {
            circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker(
                "OutboxCleanupTaskConnectivity",
                criticalErrorTriggerTime,
                ex => criticalError.Raise("Failed to clean the Outbox.", ex));

            if (deduplicationDataCleanupPeriod == Timeout.InfiniteTimeSpan)
            {
                Logger.InfoFormat("Outbox cleanup task is disabled.");
            }

            cancellationTokenSource = new CancellationTokenSource();

            // no Task.Run here because PerformCleanupAndSwallowExceptions yields with an await almost immediately
            cleanup = PerformCleanupAndSwallowExceptions(cancellationTokenSource.Token);

            return Task.CompletedTask;
        }


        protected override Task OnStop(IMessageSession busSession, CancellationToken cancellationToken = default)
        {
            cancellationTokenSource.Cancel();
            return cleanup;
        }

        async Task PerformCleanupAndSwallowExceptions(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(deduplicationDataCleanupPeriod, cancellationToken).ConfigureAwait(false);
                    await outboxPersister.RemoveEntriesOlderThan(DateTime.UtcNow - timeToKeepDeduplicationData, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
                {
                    // private token, cleaner is being stopped, log the exception in case the stack trace is ever useful for debugging
                    Logger.Debug("Operation canceled while stopping outbox cleaner.", ex);
                    break;
                }
                catch (Exception ex)
                {
                    await circuitBreaker.Failure(ex, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(OutboxCleaner));

        readonly CriticalError criticalError;
        readonly INHibernateOutboxStorage outboxPersister;

        RepeatedFailuresOverTimeCircuitBreaker circuitBreaker;

        Task cleanup;
        CancellationTokenSource cancellationTokenSource;

        TimeSpan timeToKeepDeduplicationData;
        TimeSpan deduplicationDataCleanupPeriod;
        TimeSpan criticalErrorTriggerTime;
    }
}
