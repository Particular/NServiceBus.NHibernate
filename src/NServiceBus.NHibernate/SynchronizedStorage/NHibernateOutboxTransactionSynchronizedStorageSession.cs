namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;
    using Outbox.NHibernate;
    using Persistence;

    [SkipWeaving]
    class NHibernateOutboxTransactionSynchronizedStorageSession : CompletableSynchronizedStorageSession, INHibernateSynchronizedStorageSession
    {
        NHibernateOutboxTransaction outboxTransaction;

        public NHibernateOutboxTransactionSynchronizedStorageSession(NHibernateOutboxTransaction outboxTransaction)
        {
            this.outboxTransaction = outboxTransaction;
        }

        public ISession Session => outboxTransaction.Session;
        public void OnSaveChanges(Func<SynchronizedStorageSession, Task> callback)
        {
            outboxTransaction.OnSaveChanges(() => callback(this));
        }

        public ITransaction Transaction => outboxTransaction.Transaction;

        public Task CompleteAsync() => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}