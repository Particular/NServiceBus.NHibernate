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
    using Janitor;
    using NServiceBus.NHibernate.SynchronizedStorage;
    using Outbox;
    using Outbox.NHibernate;
    using Transport;

    [SkipWeaving]
    class NHibernateSynchronizedStorageSession : ICompletableSynchronizedStorageSession
    {
        public INHibernateStorageSessionInternal Session { get; private set; }
        readonly ISessionFactory sessionFactory;

        public NHibernateSynchronizedStorageSession(SessionFactoryHolder sessionFactoryHolder)
        {
            sessionFactory = sessionFactoryHolder.SessionFactory;
        }

        public ValueTask<bool> TryOpen(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = new CancellationToken())
        {
            if (transaction is INHibernateOutboxTransaction nhibernateTransaction)
            {
                nhibernateTransaction.BeginSynchronizedSession(context);
                Session = new NHibernateOutboxTransactionSynchronizedStorageSession(nhibernateTransaction, this);
                return new ValueTask<bool>(true);
            }

            return new ValueTask<bool>(false);
        }

        public ValueTask<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken = new CancellationToken())
        {
            if (!transportTransaction.TryGet(out Transaction ambientTransaction))
            {
                return new ValueTask<bool>(false);
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
                }, this);
            Session = session;

            return new ValueTask<bool>(true);
        }

        static DbConnection OpenConnection(SessionFactoryImpl sessionFactoryImpl, Transaction ambientTransaction)
        {
            var connection = sessionFactoryImpl.ConnectionProvider.GetConnection();
            connection.EnlistTransaction(ambientTransaction);
            return connection;
        }

        public Task Open(ContextBag contextBag, CancellationToken cancellationToken = new CancellationToken())
        {
            Session = new NHibernateLazyNativeTransactionSynchronizedStorageSession(() => sessionFactory.OpenSession(), this);
            return Task.CompletedTask;
        }

        public Task CompleteAsync(CancellationToken cancellationToken = new CancellationToken()) => Session.CompleteAsync(cancellationToken);

        public void Dispose()
        {
            Session.Dispose();
        }
    }
}