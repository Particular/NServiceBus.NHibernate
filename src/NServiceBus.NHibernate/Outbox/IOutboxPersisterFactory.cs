namespace NServiceBus.NHibernate.Outbox
{
    using global::NHibernate;
    using System.Transactions;
    using NServiceBus.Outbox.NHibernate;
    using Persistence.NHibernate;

    interface IOutboxPersisterFactory
    {
        INHibernateOutboxStorage Create(ISessionFactory sessionFactory, string endpointName, bool pessimisticMode,
            bool transactionScope, System.Data.IsolationLevel adoIsolationLevel, IsolationLevel transactionScopeIsolationLevel);
    }

    class OutboxPersisterFactory<T> : IOutboxPersisterFactory
        where T : class, IOutboxRecord, new()
    {
        public INHibernateOutboxStorage Create(ISessionFactory sessionFactory, string endpointName,
            bool pessimisticMode, bool transactionScope, System.Data.IsolationLevel adoIsolationLevel,
            IsolationLevel transactionScopeIsolationLevel)
        {
            ConcurrencyControlStrategy concurrencyControlStrategy;
            if (pessimisticMode)
            {
                concurrencyControlStrategy = new PessimisticConcurrencyControlStrategy<T>();
            }
            else
            {
                concurrencyControlStrategy = new OptimisticConcurrencyControlStrategy<T>();
            }

            INHibernateOutboxTransaction transactionFactory()
            {
                return transactionScope
                    ? (INHibernateOutboxTransaction)new NHibernateTransactionScopeTransaction(concurrencyControlStrategy, sessionFactory, transactionScopeIsolationLevel)
                    : new NHibernateLocalOutboxTransaction(concurrencyControlStrategy, sessionFactory, adoIsolationLevel);
            }

            var persister = new OutboxPersister<T>(sessionFactory, transactionFactory, endpointName);
            return persister;
        }
    }
}