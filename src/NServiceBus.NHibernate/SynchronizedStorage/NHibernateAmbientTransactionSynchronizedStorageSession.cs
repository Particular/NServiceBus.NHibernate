namespace NServiceBus.Persistence.NHibernate
{
    using System.Data;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;
    using NServiceBus.Persistence;

    [SkipWeaving]
    class NHibernateAmbientTransactionSynchronizedStorageSession : CompletableSynchronizedStorageSession, INHibernateSynchronizedStorageSession
    {
        readonly IDbConnection connection;
        readonly bool ownsConnection;

        public NHibernateAmbientTransactionSynchronizedStorageSession(ISession session, IDbConnection connection, bool ownsConnection)
        {
            this.connection = connection;
            this.ownsConnection = ownsConnection;
            Session = session;
        }

        public ISession Session { get; }
        public void Dispose()
        {
            Session.Flush();
            Session.Dispose();

            if (ownsConnection)
            {
                connection.Dispose();
            }
        }

        public Task CompleteAsync()
        {
            return Task.FromResult(0);
        }
    }
}