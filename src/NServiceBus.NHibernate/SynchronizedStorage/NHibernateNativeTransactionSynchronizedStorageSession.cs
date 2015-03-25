namespace NServiceBus.Persistence.NHibernate
{
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;
    using NServiceBus.Persistence;

    [SkipWeaving]
    class NHibernateNativeTransactionSynchronizedStorageSession : CompletableSynchronizedStorageSession, INHibernateSynchronizedStorageSession
    {
        readonly bool ownsSession;

        public NHibernateNativeTransactionSynchronizedStorageSession(ISession session, ITransaction transaction, bool ownsSession)
        {
            this.ownsSession = ownsSession;
            Session = session;
            Transaction = transaction;
        }

        public ISession Session { get; }
        public ITransaction Transaction { get; }

        public Task CompleteAsync()
        {
            if (ownsSession)
            {
                Transaction.Commit();
                Transaction.Dispose();
            }
            return Task.FromResult(0);
        }

        public void Dispose()
        {
            if (ownsSession)
            {
                Session.Dispose();
            }
        }
    }
}