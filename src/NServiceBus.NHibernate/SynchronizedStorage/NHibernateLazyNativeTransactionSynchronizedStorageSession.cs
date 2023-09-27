namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;

    [SkipWeaving]
    class NHibernateLazyNativeTransactionSynchronizedStorageSession : INHibernateStorageSessionInternal
    {
        readonly ISynchronizedStorageSession synchronizedStorageSession;
        Lazy<ISession> session;
        Func<ISynchronizedStorageSession, CancellationToken, Task> onSaveChangesCallback = (_, __) => Task.CompletedTask;
        ITransaction transaction;

        public NHibernateLazyNativeTransactionSynchronizedStorageSession(Func<ISession> sessionFactory, ISynchronizedStorageSession synchronizedStorageSession)
        {
            this.synchronizedStorageSession = synchronizedStorageSession;
            session = new Lazy<ISession>(() =>
            {
                var s = sessionFactory();
                transaction = s.BeginTransaction();
                return s;
            });
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

        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            await onSaveChangesCallback(synchronizedStorageSession, cancellationToken).ConfigureAwait(false);
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