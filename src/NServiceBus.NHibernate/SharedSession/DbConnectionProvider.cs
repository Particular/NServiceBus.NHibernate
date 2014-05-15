namespace NServiceBus.NHibernate.SharedSession
{
    using System;
    using System.Data;
    using NServiceBus.Outbox;
    using Pipeline;

    class DbConnectionProvider:IDbConnectionProvider
    {
        public PipelineExecutor PipelineExecutor { get; set; }

        public string ConnectionString { get; set; }

        public IDbConnection Connection
        {
            get
            {
                IDbConnection existingConnection;

                if (!PipelineExecutor.CurrentContext.TryGet(string.Format("SqlConnection-{0}", ConnectionString), out existingConnection))
                {
                    throw new Exception("No active sql connection found");
                }

                return existingConnection;
            }
        }
    }
}