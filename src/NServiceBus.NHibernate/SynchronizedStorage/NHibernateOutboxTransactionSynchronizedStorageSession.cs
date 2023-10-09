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
    class NHibernateOutboxTransactionSynchronizedStorageSession : INHibernateStorageSessionInternal
    {
        readonly INHibernateOutboxTransaction outboxTransaction;
        readonly ISynchronizedStorageSession synchronizedStorageSession;

        public NHibernateOutboxTransactionSynchronizedStorageSession(INHibernateOutboxTransaction outboxTransaction, ISynchronizedStorageSession synchronizedStorageSession)
        {
            this.outboxTransaction = outboxTransaction;
            this.synchronizedStorageSession = synchronizedStorageSession;
        }

        public ISession Session => outboxTransaction.Session;

        public void OnSaveChanges(Func<ISynchronizedStorageSession, CancellationToken, Task> callback)
        {
            outboxTransaction.OnSaveChanges(token => callback(synchronizedStorageSession, token));
        }

        public Task CompleteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}