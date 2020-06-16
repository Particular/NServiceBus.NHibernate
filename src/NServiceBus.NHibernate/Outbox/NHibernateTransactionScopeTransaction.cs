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

        OutboxBehavior behavior;
        Func<Task> onSaveChangesCallback;
        TransactionScope transactionScope;
        Transaction ambientTransaction;
        SessionFactoryImpl sessionFactoryImpl;
        bool commit;
        DbConnection connection;

        public NHibernateTransactionScopeTransaction(OutboxBehavior behavior, ISessionFactory sessionFactory)
        {
            this.behavior = behavior;
            sessionFactoryImpl = sessionFactory as SessionFactoryImpl;
            if (sessionFactoryImpl == null)
            {
                throw new NotSupportedException("Overriding default implementation of ISessionFactory is not supported.");
            }
        }

        public ISession Session { get; private set; }

        public void OnSaveChanges(Func<Task> callback)
        {
            if (onSaveChangesCallback != null)
            {
                throw new Exception("Save changes callback for this session has already been registered.");
            }

            onSaveChangesCallback = callback;
        }

        public async Task Commit()
        {
            if (onSaveChangesCallback != null)
            {
                await onSaveChangesCallback().ConfigureAwait(false);
            }

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

        public async Task Begin(string endpointQualifiedMessageId)
        {
            transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled);
            ambientTransaction = Transaction.Current;
            Session = OpenSession();
            await behavior.Begin(endpointQualifiedMessageId, Session).ConfigureAwait(false);
            await Session.FlushAsync().ConfigureAwait(false);
        }

        public async Task Complete(string endpointQualifiedMessageId, OutboxMessage outboxMessage, ContextBag context)
        {
            await behavior.Complete(endpointQualifiedMessageId, Session, outboxMessage, context).ConfigureAwait(false);
            await Session.FlushAsync().ConfigureAwait(false);
        }

        public void BeginSynchronizedSession(ContextBag context)
        {
            if (Transaction.Current != null && Transaction.Current != ambientTransaction)
            {
                Log.Warn("The endpoint is configured to use Outbox with TransactionScope but a different TransactionScope " +
                         "has been detected in the current context. " +
                         "Do not use config.UnitOfWork().WrapHandlersInATransactionScope().");
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