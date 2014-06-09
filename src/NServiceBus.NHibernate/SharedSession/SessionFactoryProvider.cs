namespace NServiceBus.Features
{
    using global::NHibernate;
    using NHibernate.SharedSession;

    class SessionFactoryProvider:ISessionFactoryProvider
    {
        readonly ISessionFactory sessionFactory;

        public SessionFactoryProvider(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public ISessionFactory SessionFactory { get { return sessionFactory; } }
    }
}