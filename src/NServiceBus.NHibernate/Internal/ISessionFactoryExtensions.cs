namespace NServiceBus.NHibernate.Internal
{
    using System.Data;
    using global::NHibernate;
    using global::NHibernate.Impl;

    static class ISessionFactoryExtensions
    {
        internal static IDbConnection GetConnection(this ISessionFactory sessionFactory)
        {
            var sessionFactoryImpl = sessionFactory as SessionFactoryImpl;

            return sessionFactoryImpl != null ? sessionFactoryImpl.ConnectionProvider.GetConnection() : null;
        }

        internal static IStatelessSession OpenStatelessSessionEx(this ISessionFactory sessionFactory, IDbConnection connection)
        {
            if (connection == null)
            {
                return sessionFactory.OpenStatelessSession();
            }

            return sessionFactory.OpenStatelessSession(connection);
        }

        internal static ISession OpenSessionEx(this ISessionFactory sessionFactory, IDbConnection connection)
        {
            if (connection == null)
            {
                return sessionFactory.OpenSession();
            }

            return sessionFactory.OpenSession(connection);
        }
    }
}