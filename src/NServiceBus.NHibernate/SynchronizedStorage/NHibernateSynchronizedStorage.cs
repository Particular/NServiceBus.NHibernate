namespace NServiceBus.Persistence.NHibernate
{
    using System.Threading.Tasks;
    using global::NHibernate;
    using Extensibility;
    using Persistence;

    class NHibernateSynchronizedStorage : ISynchronizedStorage
    {
        ISessionFactory sessionFactory;
        CurrentSessionHolder currentSessionHolder;

        public NHibernateSynchronizedStorage(ISessionFactory sessionFactory, CurrentSessionHolder currentSessionHolder)
        {
            this.sessionFactory = sessionFactory;
            this.currentSessionHolder = currentSessionHolder;
        }

        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var session = new NHibernateLazyNativeTransactionSynchronizedStorageSession(() => sessionFactory.OpenSession());
            currentSessionHolder?.SetCurrentSession(session);
            return Task.FromResult<CompletableSynchronizedStorageSession>(session);
        }
    }
}