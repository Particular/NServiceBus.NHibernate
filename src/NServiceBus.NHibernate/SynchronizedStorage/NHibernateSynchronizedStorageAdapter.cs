namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data.SqlClient;
    using global::NHibernate;
    using global::NHibernate.Impl;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.Persistence;
    using NServiceBus.Transports;

    class NHibernateSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        ISessionFactory sessionFactory;

        public NHibernateSynchronizedStorageAdapter(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public bool TryAdapt(TransportTransaction transportTransaction, out CompletableSynchronizedStorageSession session)
        {
            System.Transactions.Transaction ambientTransaction;
            if (transportTransaction.TryGet(out ambientTransaction))
            {
                SqlConnection existingSqlConnection;
                if (transportTransaction.TryGet(out existingSqlConnection)) //SQL server transport in ambient TX mode
                {
                    session = new NHibernateAmbientTransactionSynchronizedStorageSession(sessionFactory.OpenSession(existingSqlConnection), existingSqlConnection, false);
                    return true;
                }
                else //Other transport in ambient TX mode
                {
                    var sessionFactoryImpl = sessionFactory as SessionFactoryImpl;
                    if (sessionFactoryImpl == null)
                    {
                        throw new NotSupportedException("Overriding default implementation of ISessionFactory is not supported.");
                    }
                    var connection = sessionFactoryImpl.ConnectionProvider.GetConnection();
                    session = new NHibernateAmbientTransactionSynchronizedStorageSession(sessionFactory.OpenSession(connection), connection, true);
                    return true;
                }
            }
            session = null;
            return false;
        }

        public bool TryAdapt(OutboxTransaction transaction, out CompletableSynchronizedStorageSession session)
        {
            var nhibernateTransaction = transaction as NHibernateOutboxTransaction;
            if (nhibernateTransaction != null)
            {
                session = new NHibernateNativeTransactionSynchronizedStorageSession(nhibernateTransaction.Session, nhibernateTransaction.Transaction, false);
                return true;
            }
            session = null;
            return false;
        }
    }
}