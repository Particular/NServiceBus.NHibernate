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
    using NServiceBus.NHibernate.SynchronizedStorage;
    using Outbox;
    using Outbox.NHibernate;
    using Transport;

    class NHibernateSynchronizedStorageSession : ICompletableSynchronizedStorageSession, INHibernateStorageSessionProvider
    {
        public INHibernateStorageSession InternalSession => internalSession;

        readonly ISessionFactory sessionFactory;
        INHibernateStorageSessionInternal internalSession;

        public NHibernateSynchronizedStorageSession(SessionFactoryHolder sessionFactoryHolder) => sessionFactory = sessionFactoryHolder.SessionFactory;

        public ValueTask<bool> TryOpen(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (transaction is INHibernateOutboxTransaction nhibernateTransaction)
            {
                nhibernateTransaction.BeginSynchronizedSession(context);
                internalSession = new NHibernateOutboxTransactionSynchronizedStorageSession(nhibernateTransaction, this);
                return new ValueTask<bool>(true);
            }

            return new ValueTask<bool>(false);
        }

        public ValueTask<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (!transportTransaction.TryGet(out Transaction ambientTransaction))
            {
                return new ValueTask<bool>(false);
            }
            if (sessionFactory is not SessionFactoryImpl sessionFactoryImpl)
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
                }, this);
            internalSession = session;

            return new ValueTask<bool>(true);
        }

        static DbConnection OpenConnection(SessionFactoryImpl sessionFactoryImpl, Transaction ambientTransaction)
        {
            var connection = sessionFactoryImpl.ConnectionProvider.GetConnection();
            connection.EnlistTransaction(ambientTransaction);
            return connection;
        }

        public Task Open(ContextBag contextBag, CancellationToken cancellationToken = default)
        {
            internalSession = new NHibernateLazyNativeTransactionSynchronizedStorageSession(() => sessionFactory.OpenSession(), this);
            return Task.CompletedTask;
        }

        public Task CompleteAsync(CancellationToken cancellationToken = new CancellationToken()) => internalSession.CompleteAsync(cancellationToken);

        public void Dispose() => internalSession?.Dispose();
    }
}