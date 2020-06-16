namespace NServiceBus.Outbox.NHibernate
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using global::NHibernate;
    using Janitor;
    using Logging;
    using Outbox;

    [SkipWeaving]
    class NHibernateLocalOutboxTransaction : INHibernateOutboxTransaction
    {
        static ILog Log = LogManager.GetLogger<NHibernateLocalOutboxTransaction>();

        readonly OutboxBehavior behavior;
        readonly ISessionFactory sessionFactory;

        public ISession Session { get; private set; }

        public NHibernateLocalOutboxTransaction(OutboxBehavior behavior, ISessionFactory sessionFactory)
        {
            this.behavior = behavior;
            this.sessionFactory = sessionFactory;
        }

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
            await transaction.CommitAsync().ConfigureAwait(false);
            transaction.Dispose();
            transaction = null;
        }

        public void Dispose()
        {
            //If save changes callback failed, we need to dispose the transaction here.
            if (transaction != null)
            {
                transaction.Dispose();
                transaction = null;
            }
            Session.Dispose();
        }

        Func<Task> onSaveChangesCallback;
        ITransaction transaction;

        public Task Begin(string endpointQualifiedMessageId)
        {
            Session = sessionFactory.OpenSession();
            transaction = Session.BeginTransaction();

            return behavior.Begin(endpointQualifiedMessageId, Session);
        }

        public Task Complete(string endpointQualifiedMessageId, OutboxMessage outboxMessage, ContextBag context)
        {
            return behavior.Complete(endpointQualifiedMessageId, Session, outboxMessage, context);
        }

        public void BeginSynchronizedSession(ContextBag context)
        {
            if (Transaction.Current != null)
            {
                Log.Warn("The endpoint is configured to use Outbox but a TransactionScope has been detected. " +
                         "In order to make the Outbox compatible with TransactionScope, use " +
                         "config.EnableOutbox().UseTransactionScope(). " +
                         "Do not use config.UnitOfWork().WrapHandlersInATransactionScope().");
            }
        }
    }
}