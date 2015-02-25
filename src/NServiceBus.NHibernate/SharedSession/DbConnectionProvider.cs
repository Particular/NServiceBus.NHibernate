namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data;
    using Outbox;
    using Pipeline;

    class DbConnectionProvider : IDbConnectionProvider
    {
        public PipelineExecutor PipelineExecutor { get; set; }
        public string DefaultConnectionString { get; set; }
        public bool DisableConnectionSharing { get; set; }

        public bool TryGetConnection(out IDbConnection connection, string connectionString)
        {
            if (DisableConnectionSharing)
            {
                connection = null;
                return false;
            }
            if (connectionString == null)
            {
                connectionString = DefaultConnectionString;
            }

            var result = PipelineExecutor.CurrentContext.TryGet(string.Format("SqlConnection-{0}", connectionString), out connection);

            if (result == false)
            {
                Lazy<IDbConnection> lazyConnection;

                result = PipelineExecutor.CurrentContext.TryGet(string.Format("LazySqlConnection-{0}", connectionString), out lazyConnection);
                
                if (result)
                {
                    connection = lazyConnection.Value;
                }
            }

            return result;
        }
    }
}