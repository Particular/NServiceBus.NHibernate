namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data;
    using global::NHibernate;
    using Pipeline;

    /// <summary>
    /// Provides users with access to the current NHibernate <see cref="ITransaction"/>, <see cref="IDbConnection"/> and <see cref="ISession"/>. 
    /// </summary>
    public class NHibernateStorageContext
    {
        readonly PipelineExecutor pipelineExecutor;
        readonly string connectionString;

        internal NHibernateStorageContext(PipelineExecutor pipelineExecutor, string connectionString)
        {
            this.pipelineExecutor = pipelineExecutor;
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Gets the database connection associated with the current NHibernate <see cref="Session"/>
        /// </summary>
        public IDbConnection Connection
        {
            get
            {
                Lazy<ISession> lazy;
                if (pipelineExecutor.CurrentContext.TryGet(string.Format("LazyNHibernateSession-{0}", connectionString), out lazy))
                {
                    return lazy.Value.Connection;
                }

                throw new InvalidOperationException("No connection available");
            }
        }
        
        /// <summary>
        /// Gets the database transaction associated with the current NHibernate <see cref="Session"/> or null when using TransactionScope.
        /// </summary>
        public IDbTransaction DatabaseTransaction
        {
            get
            {
                using (var command = Connection.CreateCommand())
                {
                    Lazy<ITransaction> lazy;
                    if (pipelineExecutor.CurrentContext.TryGet(string.Format("LazyNHibernateTransaction-{0}", connectionString), out lazy))
                    {
                        lazy.Value.Enlist(command);
                        return command.Transaction;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the current context NHibernate <see cref="ISession"/>.
        /// </summary>
        public ISession Session
        {
            get
            {
                Lazy<ISession> lazy;
                if (pipelineExecutor.CurrentContext.TryGet(string.Format("LazyNHibernateSession-{0}", connectionString), out lazy))
                {
                    return lazy.Value;
                }

                throw new InvalidOperationException("No session available");
            }
        }

        /// <summary>
        /// Gets the current context NHibernate <see cref="ITransaction"/>.
        /// </summary>
        public ITransaction Transaction
        {
            get
            {
                Lazy<ITransaction> lazy;
                if (pipelineExecutor.CurrentContext.TryGet(string.Format("LazyNHibernateTransaction-{0}", connectionString), out lazy))
                {
                    return lazy.Value;
                }

                throw new InvalidOperationException("No transaction available");
            }
        }
    }
}