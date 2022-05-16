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

        [ObsoleteEx(Message = "Use the overload that supports cancellation.", RemoveInVersion = "10", TreatAsErrorFromVersion = "9")]
#pragma warning disable PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
        public void OnSaveChanges(Func<ISynchronizedStorageSession, Task> callback)
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