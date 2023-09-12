namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;
    using Persistence;

    [SkipWeaving]
    class NHibernateLazyAmbientTransactionSynchronizedStorageSession : INHibernateStorageSessionInternal
    {
        readonly ISynchronizedStorageSession synchronizedStorageSession;
        Lazy<ISession> session;
        Lazy<DbConnection> connection;
        Func<ISynchronizedStorageSession, CancellationToken, Task> onSaveChangesCallback = (_, __) => Task.CompletedTask;

        public NHibernateLazyAmbientTransactionSynchronizedStorageSession(Func<DbConnection> connectionFactory, Func<DbConnection, ISession> sessionFactory, ISynchronizedStorageSession synchronizedStorageSession)
        {
            this.synchronizedStorageSession = synchronizedStorageSession;
            connection = new Lazy<DbConnection>(connectionFactory);
            session = new Lazy<ISession>(() => sessionFactory(connection.Value));
        }

        public ISession Session => session.Value;

        public void OnSaveChanges(Func<ISynchronizedStorageSession, CancellationToken, Task> callback)
        {
            var oldCallback = onSaveChangesCallback;
            onSaveChangesCallback = async (s, token) =>
            {
                await oldCallback(s, token).ConfigureAwait(false);
                await callback(s, token).ConfigureAwait(false);
            };
        }

        public void Dispose()
        {
            if (connection.IsValueCreated)
            {
                connection.Value.Dispose();
            }
        }

        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            await onSaveChangesCallback(synchronizedStorageSession, cancellationToken).ConfigureAwait(false);
            if (session.IsValueCreated)
            {
                session.Value.Flush();
                session.Value.Dispose();
            }
        }
    }
}