namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data;
    using global::NHibernate;
    using global::NHibernate.Impl;
    using Pipeline;

    static class ISessionFactoryExtensions
    {
        internal static IDbConnection GetConnection(this ISessionFactory sessionFactory, PipelineExecutor pipelineExecutor)
        {
            IDbConnection dbConnection;

            if (pipelineExecutor != null)
            {
                dbConnection = pipelineExecutor.CurrentContext.Get<IDbConnection>();

                if (dbConnection != null)
                {
                    return dbConnection;
                }
                
            }

            var sessionFactoryImpl = sessionFactory as SessionFactoryImpl;

            if (sessionFactoryImpl == null)
            {
                throw new Exception("Can't create a new connection");
            }

            dbConnection = sessionFactoryImpl.ConnectionProvider.GetConnection();

            if (pipelineExecutor != null)
            {
                pipelineExecutor.CurrentContext.Set(typeof(IDbConnection).FullName, dbConnection);    
            }
            

            return dbConnection;
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