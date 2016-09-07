namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;
    using NServiceBus.Persistence;

    [SkipWeaving]
    class NHibernateAmbientTransactionSynchronizedStorageSession : CompletableSynchronizedStorageSession, INHibernateSynchronizedStorageSession
    {
        Func<IDbConnection> connectionFactory;
        Func<IDbConnection, ISession> sessionFactory;
        ISession session;
        IDbConnection connection;

        public NHibernateAmbientTransactionSynchronizedStorageSession(Func<IDbConnection> connectionFactory, Func<IDbConnection, ISession> sessionFactory)
        {
            this.connectionFactory = connectionFactory;
            this.sessionFactory = sessionFactory;
        }

        public ISession Session
        {
            get
            {
                if (connection == null)
                {
                    connection = connectionFactory();
                }
                if (session == null)
                {
                    session = sessionFactory(connection);
                }
                return session;
            }
        }

        public void Dispose()
        {
            session?.Flush();
            session?.Dispose();
            connection?.Dispose();
        }

        public Task CompleteAsync()
        {
            return Task.FromResult(0);
        }
    }
}