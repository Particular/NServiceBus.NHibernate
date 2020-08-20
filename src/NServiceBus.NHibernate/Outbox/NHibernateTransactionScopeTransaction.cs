namespace NServiceBus.Outbox.NHibernate
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using global::NHibernate;
    using global::NHibernate.Impl;
    using Janitor;
    using Logging;

    [SkipWeaving]
    class NHibernateTransactionScopeTransaction : INHibernateOutboxTransaction
    {
        static ILog Log = LogManager.GetLogger<NHibernateTransactionScopeTransaction>();

        ConcurrencyControlStrategy concurrencyControlStrategy;
        Func<Task> onSaveChangesCallback = () => Task.CompletedTask;
        TransactionScope transactionScope;
        Transaction ambientTransaction;
        SessionFactoryImpl sessionFactoryImpl;
        bool commit;
        DbConnection connection;

        public NHibernateTransactionScopeTransaction(ConcurrencyControlStrategy concurrencyControlStrategy, ISessionFactory sessionFactory)
        {
            this.concurrencyControlStrategy = concurrencyControlStrategy;
            sessionFactoryImpl = sessionFactory as SessionFactoryImpl;
            if (sessionFactoryImpl == null)
            {
                throw new NotSupportedException("Overriding default implementation of ISessionFactory is not supported.");
            }
        }

        public ISession Session { get; private set; }

        public void OnSaveChanges(Func<Task> callback)
        {
            var oldCallback = onSaveChangesCallback;
            onSaveChangesCallback = async () =>
            {
                await oldCallback().ConfigureAwait(false);
                await callback().ConfigureAwait(false);
            };
        }

        public async Task Commit()
        {
            await onSaveChangesCallback().ConfigureAwait(false);
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
            }
            connection?.Dispose();
            if (transactionScope != null)
            {
                if (commit)
                {
                    transactionScope.Complete();
                }
                transactionScope.Dispose();
                transactionScope = null;
                ambientTransaction = null;
            }
        }

        // Prepare is deliberately kept sync to allow floating of TxScope where needed
        public void Prepare()
        {
            transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled);
            ambientTransaction = Transaction.Current;
        }

        public async Task<OutboxTransaction> Begin(string endpointQualifiedMessageId)
        {

            Session = OpenSession();
            await concurrencyControlStrategy.Begin(endpointQualifiedMessageId, Session).ConfigureAwait(false);
            await Session.FlushAsync().ConfigureAwait(false);
            return this;
        }

        public async Task Complete(string endpointQualifiedMessageId, OutboxMessage outboxMessage, ContextBag context)
        {
            await concurrencyControlStrategy.Complete(endpointQualifiedMessageId, Session, outboxMessage, context).ConfigureAwait(false);
            await Session.FlushAsync().ConfigureAwait(false);
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