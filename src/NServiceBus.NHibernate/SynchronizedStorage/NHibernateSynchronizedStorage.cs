namespace NServiceBus.Persistence.NHibernate
{
    using System.Threading.Tasks;
    using global::NHibernate;
    using Extensibility;
    using Persistence;

    class NHibernateSynchronizedStorage : ISynchronizedStorage
    {
        readonly ISessionFactory sessionFactory;

        public NHibernateSynchronizedStorage(ISessionFactory sessionFactory) => this.sessionFactory = sessionFactory;

        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var session = new NHibernateLazyNativeTransactionSynchronizedStorageSession(() => sessionFactory.OpenSession());
            return Task.FromResult<CompletableSynchronizedStorageSession>(session);
        }
    }
}