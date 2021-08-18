namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using global::NHibernate;
    using global::NHibernate.Impl;
    using Outbox;
    using Outbox.NHibernate;
    using Persistence;
    using Transport;

    class NHibernateSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        ISessionFactory sessionFactory;
        CurrentSessionHolder currentSessionHolder;
        static readonly Task<ICompletableSynchronizedStorageSession> EmptyResult = Task.FromResult((ICompletableSynchronizedStorageSession)null);

        public NHibernateSynchronizedStorageAdapter(ISessionFactory sessionFactory, CurrentSessionHolder currentSessionHolder)
        {
            this.sessionFactory = sessionFactory;
            this.currentSessionHolder = currentSessionHolder;
        }

        public Task<ICompletableSynchronizedStorageSession> TryAdapt(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (transaction is INHibernateOutboxTransaction nhibernateTransaction)
            {
                nhibernateTransaction.BeginSynchronizedSession(context);
                var session = new NHibernateOutboxTransactionSynchronizedStorageSession(nhibernateTransaction);
                currentSessionHolder?.SetCurrentSession(session);
                return Task.FromResult<ICompletableSynchronizedStorageSession>(session);
            }
            return EmptyResult;
        }

        public Task<ICompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (!transportTransaction.TryGet(out Transaction ambientTransaction))
            {
                return EmptyResult;
            }
            if (!(sessionFactory is SessionFactoryImpl sessionFactoryImpl))
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
            return Task.FromResult<ICompletableSynchronizedStorageSession>(session);
        }

        static DbConnection OpenConnection(SessionFactoryImpl sessionFactoryImpl, Transaction ambientTransaction)
        {
            var connection = sessionFactoryImpl.ConnectionProvider.GetConnection();
            connection.EnlistTransaction(ambientTransaction);
            return connection;
        }
    }
}