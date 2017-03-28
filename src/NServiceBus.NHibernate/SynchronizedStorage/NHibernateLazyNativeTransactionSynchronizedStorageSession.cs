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

        public NHibernateLazyNativeTransactionSynchronizedStorageSession(Func<ISession> sessionFactory)
        {
            session = new Lazy<ISession>(() =>
            {
                var s = sessionFactory();
                s.BeginTransaction();
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

        public ITransaction Transaction => Session.Transaction;

        public async Task CompleteAsync()
        {
            if (onSaveChangesCallback != null)
            {
                await onSaveChangesCallback(this).ConfigureAwait(false);
            }
            if (session.IsValueCreated)
            {
                Transaction.Commit();
                Transaction.Dispose();
            }
        }

        public void Dispose()
        {
            if (session.IsValueCreated)
            {
                session.Value.Dispose();
            }
        }
    }
}