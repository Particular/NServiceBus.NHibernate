namespace NServiceBus.Outbox.NHibernate
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NHibernate;
    using NServiceBus.Extensibility;
    using NServiceBus.Logging;
    using NServiceBus.Outbox;

    sealed class NHibernateLocalOutboxTransaction(
        ConcurrencyControlStrategy concurrencyControlStrategy,
        ISessionFactory sessionFactory,
        IsolationLevel isolationLevel)
        : INHibernateOutboxTransaction
    {
        static ILog Log = LogManager.GetLogger<NHibernateLocalOutboxTransaction>();

        public ISession Session { get; private set; }

        public void OnSaveChanges(Func<CancellationToken, Task> callback)
        {
            var oldCallback = onSaveChangesCallback;
            onSaveChangesCallback = async token =>
            {
                await oldCallback(token).ConfigureAwait(false);
                await callback(token).ConfigureAwait(false);
            };
        }


        public async Task Commit(CancellationToken cancellationToken = default)
        {
            await onSaveChangesCallback(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            transaction.Dispose();
            transaction = null;
        }

        public void Dispose()
        {
            //If save changes callback failed, we need to dispose the transaction here.
            transaction?.Dispose();
            transaction = null;
            Session?.Dispose();
            Session = null;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        Func<CancellationToken, Task> onSaveChangesCallback = _ => Task.CompletedTask;
        ITransaction transaction;

        public void Prepare()
        {
            //NOOP
        }

        public async Task Begin(string endpointQualifiedMessageId, CancellationToken cancellationToken = default)
        {
            Session = sessionFactory.OpenSession();
            transaction = Session.BeginTransaction(isolationLevel);

            await concurrencyControlStrategy.Begin(endpointQualifiedMessageId, Session, cancellationToken).ConfigureAwait(false);
        }

        public Task Complete(string endpointQualifiedMessageId, OutboxMessage outboxMessage, ContextBag context, CancellationToken cancellationToken = default)
            => concurrencyControlStrategy.Complete(endpointQualifiedMessageId, Session, outboxMessage, context, cancellationToken);

        public void BeginSynchronizedSession(ContextBag context)
        {
            if (System.Transactions.Transaction.Current != null)
            {
                Log.Warn("The endpoint is configured to use Outbox but a TransactionScope has been detected. " +
                         "In order to make the Outbox compatible with TransactionScope, use " +
                         "config.EnableOutbox().UseTransactionScope(). " +
                         "Remove any custom TransactionScope added to the pipeline.");
            }
        }
    }
}