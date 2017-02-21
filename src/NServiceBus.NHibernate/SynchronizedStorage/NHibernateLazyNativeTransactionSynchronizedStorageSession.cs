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
        CallbackList callbacks = new CallbackList();

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

        public void RegisterCommitHook(Func<Task> callback)
        {
            callbacks.Add(callback);
        }

        public ITransaction Transaction => Session.Transaction;

        public async Task CompleteAsync()
        {
            await callbacks.InvokeAll().ConfigureAwait(false);
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