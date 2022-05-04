namespace NServiceBus.NHibernate.SynchronizedStorage
{
    using global::NHibernate;

    class SessionFactoryHolder
    {
        public SessionFactoryHolder(ISessionFactory sessionFactory)
        {
            SessionFactory = sessionFactory;
        }

        public ISessionFactory SessionFactory { get; }
    }
}