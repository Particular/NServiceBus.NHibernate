namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;
    using System.Transactions;
    using global::NHibernate;
    using global::NHibernate.Impl;
    using Extensibility;
    using Outbox;
    using Outbox.NHibernate;
    using Persistence;
    using Transport;

    class NHibernateSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        ISessionFactory sessionFactory;
        CurrentSessionHolder currentSessionHolder;
        static readonly Task<CompletableSynchronizedStorageSession> EmptyResult = Task.FromResult((CompletableSynchronizedStorageSession)null);

        public NHibernateSynchronizedStorageAdapter(ISessionFactory sessionFactory, CurrentSessionHolder currentSessionHolder)
        {
            this.sessionFactory = sessionFactory;
            this.currentSessionHolder = currentSessionHolder;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            if (transaction is INHibernateOutboxTransaction nhibernateTransaction)
            {
                nhibernateTransaction.BeginSynchronizedSession(context);
                var session = new NHibernateOutboxTransactionSynchronizedStorageSession(nhibernateTransaction);
                currentSessionHolder?.SetCurrentSession(session);
                return Task.FromResult<CompletableSynchronizedStorageSession>(session);
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
            var session = new NHibernateLazyAmbientTransactionSynchronizedStorageSession(
                connectionFactory: () => OpenConnection(sessionFactoryImpl, ambientTransaction),
                sessionFactory: conn =>
                {
                    var sessionBuilder = sessionFactory.WithOptions();
                    sessionBuilder.Connection(conn);
                    return sessionBuilder.OpenSession();
                });

            currentSessionHolder?.SetCurrentSession(session);
            return Task.FromResult<CompletableSynchronizedStorageSession>(session);
        }

        static DbConnection OpenConnection(SessionFactoryImpl sessionFactoryImpl, Transaction ambientTransaction)
        {
            var connection = sessionFactoryImpl.ConnectionProvider.GetConnection();
            connection.EnlistTransaction(ambientTransaction);
            return connection;
        }
    }
}