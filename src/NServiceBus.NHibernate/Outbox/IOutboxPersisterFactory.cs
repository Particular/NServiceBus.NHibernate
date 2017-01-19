namespace NServiceBus.NHibernate.Outbox
{
    using global::NHibernate;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.Persistence.NHibernate;

    interface IOutboxPersisterFactory
    {
        INHibernateOutboxStorage Create(ISessionFactory sessionFactory, string endpointName);
    }

    class OutboxPersisterFactory<T> : IOutboxPersisterFactory
        where T : class, IOutboxRecord, new()
    {
        public INHibernateOutboxStorage Create(ISessionFactory sessionFactory, string endpointName)
        {
            var persister = new OutboxPersister<T>(sessionFactory, endpointName);
            return persister;
        }
    }
}