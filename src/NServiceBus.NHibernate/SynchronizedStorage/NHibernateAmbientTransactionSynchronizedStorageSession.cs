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
        Func<SynchronizedStorageSession, Task> onSaveChangesCallback;

        public NHibernateLazyAmbientTransactionSynchronizedStorageSession(Func<IDbConnection> connectionFactory, Func<IDbConnection, ISession> sessionFactory)
        {
            connection = new Lazy<IDbConnection>(connectionFactory);
            session = new Lazy<ISession>(() => sessionFactory(connection.Value));
        }

        public ISession Session => session.Value;
        public void OnSaveChanges(Func<SynchronizedStorageSession, Task> callback)
        {
            if (onSaveChangesCallback != null)
            {
                throw new Exception("Save changes callback for this session has already been registered.");
            }
            onSaveChangesCallback = callback;
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
            if (onSaveChangesCallback != null)
            {
                await onSaveChangesCallback(this).ConfigureAwait(false);
            }
            if (session.IsValueCreated)
            {
                session.Value.Flush();
                session.Value.Dispose();
            }
        }
    }
}