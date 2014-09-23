namespace NServiceBus.Persistence.NHibernate
{
    using global::NHibernate;

    class SessionFactoryProvider
    {
        readonly ISessionFactory sessionFactory;

        public SessionFactoryProvider(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public ISessionFactory SessionFactory { get { return sessionFactory; } }
    }
}