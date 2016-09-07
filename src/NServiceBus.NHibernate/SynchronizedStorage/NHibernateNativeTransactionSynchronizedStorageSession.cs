namespace NServiceBus.Persistence.NHibernate
{
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;
    using NServiceBus.Persistence;

    [SkipWeaving]
    class NHibernateNativeTransactionSynchronizedStorageSession : CompletableSynchronizedStorageSession, INHibernateSynchronizedStorageSession
    {
        public NHibernateNativeTransactionSynchronizedStorageSession(ISession session, ITransaction transaction)
        {
            Session = session;
            Transaction = transaction;
        }

        public ISession Session { get; }
        public ITransaction Transaction { get; }

        public Task CompleteAsync()
        {
            return Task.FromResult(0);
        }

        public void Dispose()
        {
        }
    }
}