namespace NServiceBus.Outbox.NHibernate
{
    using System;
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using global::NHibernate;
    using global::NHibernate.Impl;
    using Logging;

    sealed class NHibernateTransactionScopeTransaction : INHibernateOutboxTransaction
    {
        static readonly ILog Log = LogManager.GetLogger<NHibernateTransactionScopeTransaction>();

        readonly ConcurrencyControlStrategy concurrencyControlStrategy;
        readonly IsolationLevel isolationLevel;
        Func<CancellationToken, Task> onSaveChangesCallback = _ => Task.CompletedTask;
        TransactionScope transactionScope;
        Transaction ambientTransaction;
        readonly SessionFactoryImpl sessionFactoryImpl;
        bool commit;
        DbConnection connection;

        public NHibernateTransactionScopeTransaction(ConcurrencyControlStrategy concurrencyControlStrategy,
            ISessionFactory sessionFactory, IsolationLevel isolationLevel)
        {
            this.concurrencyControlStrategy = concurrencyControlStrategy;
            this.isolationLevel = isolationLevel;
            sessionFactoryImpl = sessionFactory as SessionFactoryImpl;
            if (sessionFactoryImpl == null)
            {
                throw new NotSupportedException("Overriding default implementation of ISessionFactory is not supported.");
            }
        }

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
            commit = true;
        }

        public void Dispose()
        {
            if (Session != null)
            {
                if (commit)
                {
                    Session.Flush();
                }
                Session.Dispose();
                Session = null;
            }
            connection?.Dispose();

            if (transactionScope == null)
            {
                return;
            }

            if (commit)
            {
                transactionScope.Complete();
            }

            transactionScope.Dispose();
            transactionScope = null;
            ambientTransaction = null;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        // Prepare is deliberately kept sync to allow floating of TxScope where needed
        public void Prepare()
        {
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = isolationLevel
            };
            transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, transactionOptions, TransactionScopeAsyncFlowOption.Enabled);
            ambientTransaction = Transaction.Current;
        }

        public async Task Begin(string endpointQualifiedMessageId, CancellationToken cancellationToken = default)
        {
            Session = OpenSession();
            await concurrencyControlStrategy.Begin(endpointQualifiedMessageId, Session, cancellationToken).ConfigureAwait(false);
            await Session.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task Complete(string endpointQualifiedMessageId, OutboxMessage outboxMessage, ContextBag context, CancellationToken cancellationToken = default)
        {
            await concurrencyControlStrategy.Complete(endpointQualifiedMessageId, Session, outboxMessage, context, cancellationToken).ConfigureAwait(false);
            await Session.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public void BeginSynchronizedSession(ContextBag context)
        {
            if (Transaction.Current != null && Transaction.Current != ambientTransaction)
            {
                Log.Warn("The endpoint is configured to use Outbox with TransactionScope but a different TransactionScope " +
                         "has been detected in the current context. " +
                         "Remove any custom TransactionScope added to the pipeline.");
            }
        }

        ISession OpenSession()
        {
            var sessionBuilder = sessionFactoryImpl.WithOptions();
            connection = sessionFactoryImpl.ConnectionProvider.GetConnection();
            connection.EnlistTransaction(ambientTransaction);
            sessionBuilder.Connection(connection);
            return sessionBuilder.OpenSession();
        }
    }
}