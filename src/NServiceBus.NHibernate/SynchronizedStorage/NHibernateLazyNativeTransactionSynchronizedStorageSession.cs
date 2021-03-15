namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;

    [SkipWeaving]
    class NHibernateLazyNativeTransactionSynchronizedStorageSession : CompletableSynchronizedStorageSession, INHibernateStorageSession
    {
        Lazy<ISession> session;
        Func<SynchronizedStorageSession, Task> onSaveChangesCallback = s => Task.CompletedTask;
        ITransaction transaction;

        public NHibernateLazyNativeTransactionSynchronizedStorageSession(Func<ISession> sessionFactory)
        {
            session = new Lazy<ISession>(() =>
            {
                var s = sessionFactory();
                transaction = s.BeginTransaction();
                return s;
            });
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

        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            await onSaveChangesCallback(this).ConfigureAwait(false);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                transaction.Dispose();
                transaction = null;
            }
        }

        public void Dispose()
        {
            //If save changes callback failed, we need to dispose the transaction here.
            if (transaction != null)
            {
                transaction.Dispose();
                transaction = null;
            }
            if (session.IsValueCreated)
            {
                session.Value.Dispose();
            }
        }
    }
}