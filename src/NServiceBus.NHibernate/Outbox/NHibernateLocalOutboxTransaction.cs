namespace NServiceBus.Outbox.NHibernate
{
    using System.Data;
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using global::NHibernate;
    using Janitor;
    using NServiceBus.Logging;
    using NServiceBus.Outbox;

    [SkipWeaving]
    class NHibernateLocalOutboxTransaction : INHibernateOutboxTransaction
    {
        static ILog Log = LogManager.GetLogger<NHibernateLocalOutboxTransaction>();

        readonly ConcurrencyControlStrategy concurrencyControlStrategy;
        readonly ISessionFactory sessionFactory;
        readonly IsolationLevel isolationLevel;

        public ISession Session { get; private set; }

        public NHibernateLocalOutboxTransaction(ConcurrencyControlStrategy concurrencyControlStrategy,
            ISessionFactory sessionFactory, IsolationLevel isolationLevel)
        {
            this.concurrencyControlStrategy = concurrencyControlStrategy;
            this.sessionFactory = sessionFactory;
            this.isolationLevel = isolationLevel;
        }

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

        Func<Task> onSaveChangesCallback = () => Task.CompletedTask;
        ITransaction transaction;

        public void Prepare()
        {
            //NOOP
        }

        public async Task Begin(string endpointQualifiedMessageId)
        {
            Session = sessionFactory.OpenSession();
            transaction = Session.BeginTransaction(isolationLevel);

            await concurrencyControlStrategy.Begin(endpointQualifiedMessageId, Session).ConfigureAwait(false);
        }

        public Task Complete(string endpointQualifiedMessageId, OutboxMessage outboxMessage, ContextBag context)
        {
            return concurrencyControlStrategy.Complete(endpointQualifiedMessageId, Session, outboxMessage, context);
        }

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