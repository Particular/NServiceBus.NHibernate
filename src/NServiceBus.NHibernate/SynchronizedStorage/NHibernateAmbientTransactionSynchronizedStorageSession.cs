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
    class NHibernateLazyAmbientTransactionSynchronizedStorageSession : CompletableSynchronizedStorageSession, INHibernateStorageSession
    {
        Lazy<ISession> session;
        Lazy<DbConnection> connection;
        Func<SynchronizedStorageSession, CancellationToken, Task> onSaveChangesCallback = (_, __) => Task.CompletedTask;

        public NHibernateLazyAmbientTransactionSynchronizedStorageSession(Func<DbConnection> connectionFactory, Func<DbConnection, ISession> sessionFactory)
        {
            connection = new Lazy<DbConnection>(connectionFactory);
            session = new Lazy<ISession>(() => sessionFactory(connection.Value));
        }

        public ISession Session => session.Value;

        public void OnSaveChanges(Func<SynchronizedStorageSession, CancellationToken, Task> callback)
        {
            var oldCallback = onSaveChangesCallback;
            onSaveChangesCallback = async (s, token) =>
            {
                await oldCallback(s, token).ConfigureAwait(false);
                await callback(s, token).ConfigureAwait(false);
            };
        }

        [ObsoleteEx(Message = "Use the overload that supports cancellation.", RemoveInVersion = "10", TreatAsErrorFromVersion = "9")]
        public void OnSaveChanges(Func<SynchronizedStorageSession, Task> callback)
        {
            throw new NotImplementedException();
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
            await onSaveChangesCallback(this, cancellationToken).ConfigureAwait(false);
            if (session.IsValueCreated)
            {
                session.Value.Flush();
                session.Value.Dispose();
            }
        }
    }
}