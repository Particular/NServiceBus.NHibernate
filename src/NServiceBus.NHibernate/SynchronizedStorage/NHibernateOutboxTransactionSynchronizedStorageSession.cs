namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.Persistence;

    [SkipWeaving]
    class NHibernateOutboxTransactionSynchronizedStorageSession : CompletableSynchronizedStorageSession, INHibernateSynchronizedStorageSession
    {
        NHibernateOutboxTransaction outboxTransaction;

        public NHibernateOutboxTransactionSynchronizedStorageSession(NHibernateOutboxTransaction outboxTransaction)
        {
            this.outboxTransaction = outboxTransaction;
        }

        public ISession Session => outboxTransaction.Session;
        public void OnSaveChanges(Func<Task> callback)
        {
            outboxTransaction.OnSaveChanges(callback);
        }

        public ITransaction Transaction => outboxTransaction.Transaction;

        public Task CompleteAsync()
        {
            return Task.FromResult(0);
        }

        public void Dispose()
        {
        }
    }
}