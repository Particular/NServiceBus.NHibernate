namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;

    [SkipWeaving]
    class NHibernateLazyNativeTransactionSynchronizedStorageSession : CompletableSynchronizedStorageSession, INHibernateSynchronizedStorageSession
    {
        Func<ISession> sessionFactory;
        ISession session;
        ITransaction transaction;

        public NHibernateLazyNativeTransactionSynchronizedStorageSession(Func<ISession> sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public ISession Session
        {
            get
            {
                if (session == null)
                {
                    session = sessionFactory();
                    transaction = session.BeginTransaction();
                }
                return session;
            }
        }

        public ITransaction Transaction => Session.Transaction;

        public Task CompleteAsync()
        {
            transaction?.Commit();
            transaction?.Dispose();
            return Task.FromResult(0);
        }

        public void Dispose()
        {
            session?.Dispose();
        }
    }
}