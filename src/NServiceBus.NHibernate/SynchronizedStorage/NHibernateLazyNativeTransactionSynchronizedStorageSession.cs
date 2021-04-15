namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;

    [SkipWeaving]
    class NHibernateLazyNativeTransactionSynchronizedStorageSession : CompletableSynchronizedStorageSession, INHibernateStorageSession
    {
        Lazy<ISession> session;
        Func<SynchronizedStorageSession, CancellationToken, Task> onSaveChangesCallback = (_, __) => Task.CompletedTask;
        ITransaction transaction;

        public NHibernateLazyNativeTransactionSynchronizedStorageSession(Func<ISession> sessionFactory)
        {
            session = new Lazy<ISession>(() =>
            {
                var s = sessionFactory();
                transaction = s.BeginTransaction();
                return s;
            });
        }

        public ISession Session => session.Value;

        public void OnSaveChanges(Func<SynchronizedStorageSession, CancellationToken, Task> callback)
        {
            var oldCallback = onSaveChangesCallback;
            onSaveChangesCallback = async (s, token) =>
            {
                await oldCallback(s, token).ConfigureAwait(false);
                await callback(s, token).ConfigureAwait(false);
            };
        }

        [ObsoleteEx(Message = "Use the overload that supports cancellation.", RemoveInVersion = "10", TreatAsErrorFromVersion = "9")]
#pragma warning disable PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
        public void OnSaveChanges(Func<SynchronizedStorageSession, Task> callback)
#pragma warning restore PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
        {
            throw new NotImplementedException();
        }

        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            await onSaveChangesCallback(this, cancellationToken).ConfigureAwait(false);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                transaction.Dispose();
                transaction = null;
            }
        }

        public void Dispose()
        {
            //If save changes callback failed, we need to dispose the transaction here.
            if (transaction != null)
            {
                transaction.Dispose();
                transaction = null;
            }
            if (session.IsValueCreated)
            {
                session.Value.Dispose();
            }
        }
    }
}