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
#pragma warning disable PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
        public void OnSaveChanges(Func<SynchronizedStorageSession, Task> callback)
#pragma warning restore PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
        {
            throw new NotImplementedException();
        }

        public Task CompleteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}