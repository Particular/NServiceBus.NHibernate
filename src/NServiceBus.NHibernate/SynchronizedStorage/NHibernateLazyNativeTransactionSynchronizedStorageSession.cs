namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;

    [SkipWeaving]
    class NHibernateLazyNativeTransactionSynchronizedStorageSession : CompletableSynchronizedStorageSession, INHibernateSynchronizedStorageSession
    {
        Lazy<ISession> session;
        Func<SynchronizedStorageSession, Task> onSaveChangesCallback;
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

        public void OnSaveChanges(Func<SynchronizedStorageSession, Task> callback)
        {
            if (onSaveChangesCallback != null)
            {
                throw new Exception("Save changes callback for this session has already been registered.");
            }
            onSaveChangesCallback = callback;
        }

        public async Task CompleteAsync()
        {
            if (onSaveChangesCallback != null)
            {
                await onSaveChangesCallback(this).ConfigureAwait(false);
            }
            if (transaction != null)
            {
                await transaction.CommitAsync().ConfigureAwait(false);
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