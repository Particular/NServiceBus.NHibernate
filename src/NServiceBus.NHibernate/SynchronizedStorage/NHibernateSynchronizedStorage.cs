namespace NServiceBus.Persistence.NHibernate
{
    using System.Threading.Tasks;
    using global::NHibernate;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;

    class NHibernateSynchronizedStorage : ISynchronizedStorage
    {
        ISessionFactory sessionFactory;

        public NHibernateSynchronizedStorage(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var session = sessionFactory.OpenSession();
            var transaction = session.BeginTransaction();

            CompletableSynchronizedStorageSession result = new NHibernateNativeTransactionSynchronizedStorageSession(session, transaction, true);
            return Task.FromResult(result);
        }
    }
}