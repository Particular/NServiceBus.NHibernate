namespace NServiceBus.Persistence.NHibernate
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using global::NHibernate;
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

        public Task<ICompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag, CancellationToken cancellationToken = default)
        {
            var session = new NHibernateLazyNativeTransactionSynchronizedStorageSession(() => sessionFactory.OpenSession());
            currentSessionHolder?.SetCurrentSession(session);
            return Task.FromResult<ICompletableSynchronizedStorageSession>(session);
        }
    }
}