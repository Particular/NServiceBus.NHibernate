namespace NServiceBus.NHibernate.SharedSession
{
    using System;
    using System.Data;
    using Outbox;
    using Pipeline;

    class DbConnectionProvider : IDbConnectionProvider
    {
        public PipelineExecutor PipelineExecutor { get; set; }

        public string ConnectionString { get; set; }

        public IDbConnection Connection
        {
            get
            {
                IDbConnection existingConnection;
                Lazy<IDbConnection> lazyExistingConnection;

                if (PipelineExecutor.CurrentContext.TryGet(string.Format("SqlConnection-{0}", ConnectionString), out existingConnection))
                {
                    return existingConnection;
                }

                if (!PipelineExecutor.CurrentContext.TryGet(string.Format("LazySqlConnection-{0}", ConnectionString), out lazyExistingConnection))
                {
                    throw new Exception("No active sql connection found");
                }

                return lazyExistingConnection.Value;
            }
        }

        public bool TryGetConnection(out IDbConnection connection)
        {
            var result = PipelineExecutor.CurrentContext.TryGet(string.Format("SqlConnection-{0}", ConnectionString), out connection);

            if (result == false)
            {
                Lazy<IDbConnection> lazyConnection;
                
                result = PipelineExecutor.CurrentContext.TryGet(string.Format("LazySqlConnection-{0}", ConnectionString), out lazyConnection);
                
                if (result)
                {
                    connection = lazyConnection.Value;
                }
            }

            return result;
        }
    }
}