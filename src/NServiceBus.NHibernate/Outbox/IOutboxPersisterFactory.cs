namespace NServiceBus.NHibernate.Outbox
{
    using global::NHibernate;
    using NServiceBus.Outbox.NHibernate;
    using Persistence.NHibernate;

    interface IOutboxPersisterFactory
    {
        INHibernateOutboxStorage Create(ISessionFactory sessionFactory, string endpointName, bool pessimisticMode, bool transactionScope);
    }

    class OutboxPersisterFactory<T> : IOutboxPersisterFactory
        where T : class, IOutboxRecord, new()
    {
        public INHibernateOutboxStorage Create(ISessionFactory sessionFactory, string endpointName, bool pessimisticMode, bool transactionScope)
        {
            OutboxBehavior behavior;
            if (pessimisticMode)
            {
                behavior = new PessimisticOutboxBehavior<T>();
            }
            else
            {
                behavior = new OptimisticOutboxBehavior<T>();
            }

            INHibernateOutboxTransaction transactionFactory()
            {
                return transactionScope
                    ? (INHibernateOutboxTransaction)new NHibernateTransactionScopeTransaction(behavior, sessionFactory)
                    : new NHibernateLocalOutboxTransaction(behavior, sessionFactory);
            }

            var persister = new OutboxPersister<T>(sessionFactory, transactionFactory, endpointName);
            return persister;
        }
    }
}