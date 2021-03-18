namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;
    using Outbox.NHibernate;
    using Persistence;

    [SkipWeaving]
    class NHibernateOutboxTransactionSynchronizedStorageSession : CompletableSynchronizedStorageSession, INHibernateStorageSession
    {
        INHibernateOutboxTransaction outboxTransaction;

        public NHibernateOutboxTransactionSynchronizedStorageSession(INHibernateOutboxTransaction outboxTransaction)
        {
            this.outboxTransaction = outboxTransaction;
        }

        public ISession Session => outboxTransaction.Session;

        public void OnSaveChanges(Func<SynchronizedStorageSession, CancellationToken, Task> callback)
        {
            outboxTransaction.OnSaveChanges(token => callback(this, token));
        }

        [ObsoleteEx(Message = "Use the overload that supports cancellation.", RemoveInVersion = "10", TreatAsErrorFromVersion = "9")]
        public void OnSaveChanges(Func<SynchronizedStorageSession, Task> callback)
        {
            throw new NotImplementedException();
        }

        public Task CompleteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}