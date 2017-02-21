namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;
    using NServiceBus.Persistence;

    [SkipWeaving]
    class NHibernateLazyAmbientTransactionSynchronizedStorageSession : CompletableSynchronizedStorageSession, INHibernateSynchronizedStorageSession
    {
        Lazy<ISession> session;
        Lazy<IDbConnection> connection;
        CallbackList callbacks = new CallbackList();

        public NHibernateLazyAmbientTransactionSynchronizedStorageSession(Func<IDbConnection> connectionFactory, Func<IDbConnection, ISession> sessionFactory)
        {
            connection = new Lazy<IDbConnection>(connectionFactory);
            session = new Lazy<ISession>(() => sessionFactory(connection.Value));
        }

        public ISession Session => session.Value;
        public void RegisterCommitHook(Func<Task> callback)
        {
            callbacks.Add(callback);
        }

        public void Dispose()
        {
            if (connection.IsValueCreated)
            {
                connection.Value.Dispose();
            }
        }

        public async Task CompleteAsync()
        {
            await callbacks.InvokeAll().ConfigureAwait(false);
            if (session.IsValueCreated)
            {
                session.Value.Flush();
                session.Value.Dispose();
            }
        }
    }
}