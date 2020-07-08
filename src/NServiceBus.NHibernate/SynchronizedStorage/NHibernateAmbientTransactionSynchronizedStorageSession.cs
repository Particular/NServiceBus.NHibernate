namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;
    using Persistence;

    [SkipWeaving]
    class NHibernateLazyAmbientTransactionSynchronizedStorageSession : CompletableSynchronizedStorageSession, INHibernateStorageSession
    {
        Lazy<ISession> session;
        Lazy<DbConnection> connection;
        Func<SynchronizedStorageSession, Task> onSaveChangesCallback = storageSession => Task.CompletedTask;

        public NHibernateLazyAmbientTransactionSynchronizedStorageSession(Func<DbConnection> connectionFactory, Func<DbConnection, ISession> sessionFactory)
        {
            connection = new Lazy<DbConnection>(connectionFactory);
            session = new Lazy<ISession>(() => sessionFactory(connection.Value));
        }

        public ISession Session => session.Value;
        public void OnSaveChanges(Func<SynchronizedStorageSession, Task> callback)
        {
            var oldCallback = onSaveChangesCallback;
            onSaveChangesCallback = async s =>
            {
                await oldCallback(s).ConfigureAwait(false);
                await callback(s).ConfigureAwait(false);
            };
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
            await onSaveChangesCallback(this).ConfigureAwait(false);
            if (session.IsValueCreated)
            {
                session.Value.Flush();
                session.Value.Dispose();
            }
        }
    }
}