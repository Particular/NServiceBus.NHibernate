namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Threading.Tasks;
    using System.Transactions;
    using global::NHibernate;
    using global::NHibernate.Impl;
    using Extensibility;
    using Logging;
    using Outbox;
    using Outbox.NHibernate;
    using Persistence;
    using Transport;

    class NHibernateSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        ISessionFactory sessionFactory;
        static readonly Task<CompletableSynchronizedStorageSession> EmptyResult = Task.FromResult((CompletableSynchronizedStorageSession)null);
        static ILog Log = LogManager.GetLogger<NHibernateSynchronizedStorageAdapter>();

        public NHibernateSynchronizedStorageAdapter(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            var nhibernateTransaction = transaction as NHibernateOutboxTransaction;
            if (nhibernateTransaction != null)
            {
                if (Transaction.Current != null)
                {
                    Log.Warn("The endpoint is configured to use Outbox but a TransactionScope has been detected. Outbox mode is not compatible with "
                        + "TransactionScope. Do not use config.UnitOfWork().WrapHandlersInATransactionScope() when Outbox is enabled.");
                }

                CompletableSynchronizedStorageSession session = new NHibernateOutboxTransactionSynchronizedStorageSession(nhibernateTransaction);
                return Task.FromResult(session);
            }
            return EmptyResult;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
        {
            Transaction ambientTransaction;
            if (!transportTransaction.TryGet(out ambientTransaction))
            {
                return EmptyResult;
            }
            var sessionFactoryImpl = sessionFactory as SessionFactoryImpl;
            if (sessionFactoryImpl == null)
            {
                throw new NotSupportedException("Overriding default implementation of ISessionFactory is not supported.");
            }
            CompletableSynchronizedStorageSession session = new NHibernateLazyAmbientTransactionSynchronizedStorageSession(
                connectionFactory: () => OpenConnection(sessionFactoryImpl, ambientTransaction),
                sessionFactory: conn =>
                {
                    var sessionBuilder = sessionFactory.WithOptions();
                    sessionBuilder.Connection(conn);
                    return sessionBuilder.OpenSession();
                });
            return Task.FromResult(session);
        }

        static DbConnection OpenConnection(SessionFactoryImpl sessionFactoryImpl, Transaction ambientTransaction)
        {
            var connection = sessionFactoryImpl.ConnectionProvider.GetConnection();
            connection.EnlistTransaction(ambientTransaction);
            return connection;
        }
    }
}