namespace NServiceBus.NHibernate.SharedSession
{
    using System;
    using System.Data;
    using global::NHibernate;
    using Outbox;
    using Pipeline;

    class StorageSessionProvider : IStorageSessionProvider
    {
        public PipelineExecutor PipelineExecutor { get; set; }

        public string ConnectionString { get; set; }

        public ISessionFactoryProvider SessionFactoryProvider { get; set; }

        public IDbConnectionProvider DbConnectionProvider { get; set; }

        public ISession Session
        {
            get
            {
                Lazy<ISession> existingSession;

                if (!PipelineExecutor.CurrentContext.TryGet(string.Format("LazyNHibernateSession-{0}", ConnectionString), out existingSession))
                {
                    throw new Exception("No active storage session found in context");
                }

                return existingSession.Value;
            }
        }

        public IStatelessSession OpenStatelessSession()
        {
            IDbConnection connection;

            if (DbConnectionProvider.TryGetConnection(out connection, ConnectionString))
            {
                return SessionFactoryProvider.SessionFactory.OpenStatelessSession(connection);
            }

            return SessionFactoryProvider.SessionFactory.OpenStatelessSession();
        }
    }
}