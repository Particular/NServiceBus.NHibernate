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

        public NHibernateLazyNativeTransactionSynchronizedStorageSession(Func<ISession> sessionFactory)
        {
            session =new Lazy<ISession>(() =>
            {
                var s = sessionFactory();
                s.BeginTransaction();
                return s;
            });
        }

        public ISession Session => session.Value;

        public ITransaction Transaction => Session.Transaction;

        public Task CompleteAsync()
        {
            if (session.IsValueCreated)
            {
                Transaction.Commit();
                Transaction.Dispose();
            }
            return Task.FromResult(0);
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