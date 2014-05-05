namespace NServiceBus.Outbox
{
    using System;
    using System.Data;
    using global::NHibernate;
    using global::NHibernate.Impl;
    using Pipeline;
    using Pipeline.Contexts;

    class IDbConnectionBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        readonly PipelineExecutor pipelineExecutor;
        public ISessionFactory SessionFactory { get; set; }

        public IDbConnectionBehavior(PipelineExecutor pipelineExecutor)
        {
            this.pipelineExecutor = pipelineExecutor;
        }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            var dbConnection = pipelineExecutor.CurrentContext.Get<IDbConnection>();

            if (dbConnection == null)
            {
                var sessionFactoryImpl = SessionFactory as SessionFactoryImpl;

                if (sessionFactoryImpl != null)
                {
                    dbConnection = sessionFactoryImpl.ConnectionProvider.GetConnection();
                }
            }

            var connection = new WrappedIDbConnection(dbConnection);

            pipelineExecutor.CurrentContext.Set(typeof(IDbConnection).FullName, connection);

            try
            {
                next();
            }
            finally
            {
                pipelineExecutor.CurrentContext.Remove<IDbConnection>();
                connection.DisposeInternal();
            }
        }
    }

    class WrappedIDbConnection : IDbConnection
    {
        readonly IDbConnection connection;

        public WrappedIDbConnection(IDbConnection connection)
        {
            this.connection = connection;
        }

        public void DisposeInternal()
        {
            connection.Dispose();
        }

        public void Dispose()
        {
            
        }

        public IDbTransaction BeginTransaction()
        {
            return connection.BeginTransaction();
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return connection.BeginTransaction(il);
        }

        public void Close()
        {
            connection.Close();
        }

        public void ChangeDatabase(string databaseName)
        {
            connection.ChangeDatabase(databaseName);
        }

        public IDbCommand CreateCommand()
        {
            return connection.CreateCommand();
        }

        public void Open()
        {
            connection.Open();
        }

        public string ConnectionString
        {
            get { return connection.ConnectionString; }
            set { connection.ConnectionString = value; }
        }

        public int ConnectionTimeout
        {
            get { return connection.ConnectionTimeout; }
        }

        public string Database
        {
            get { return connection.Database; }
        }

        public ConnectionState State
        {
            get { return connection.State; }
        }
    }
}