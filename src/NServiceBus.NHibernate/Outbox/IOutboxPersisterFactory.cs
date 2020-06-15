namespace NServiceBus.NHibernate.Outbox
{
    using global::NHibernate;
    using NServiceBus.Outbox.NHibernate;
    using Persistence.NHibernate;

    interface IOutboxPersisterFactory
    {
        INHibernateOutboxStorage Create(ISessionFactory sessionFactory, string endpointName, bool pessimisticMode);
    }

    class OutboxPersisterFactory<T> : IOutboxPersisterFactory
        where T : class, IOutboxRecord, new()
    {
        public INHibernateOutboxStorage Create(ISessionFactory sessionFactory, string endpointName, bool pessimisticMode)
        {
            INHibernateOutboxTransaction transactionFactory(ISession session, ITransaction transaction)
            {
                return pessimisticMode
                    ? (INHibernateOutboxTransaction)new NHibernatePessimisticOutboxTransaction(session, transaction)
                    : new NHibernateOptimisticOutboxTransaction(session, transaction);
            }

            var persister = new OutboxPersister<T>(sessionFactory, transactionFactory, endpointName);
            return persister;
        }
    }
}