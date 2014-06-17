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
        /// Gets the current context NHibernate <see cref="IDbConnection"/> or <code>null</code> if no current context NHibernate <see cref="IDbConnection"/> available.
        /// </summary>
        public IDbConnection Connection
        {
            get
            {
                Lazy<IDbConnection> lazy;
                if (pipelineExecutor.CurrentContext.TryGet(string.Format("LazySqlConnection-{0}", connectionString), out lazy))
                {
                    return lazy.Value;
                }

                throw new InvalidOperationException("No connection available");
            }
        }

        /// <summary>
        /// Gets the current context NHibernate <see cref="ISession"/> or <code>null</code> if no current context NHibernate <see cref="ISession"/> available.
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
        /// Gets the current context NHibernate <see cref="ITransaction"/> or <code>null</code> if no current context NHibernate <see cref="ITransaction"/> available.
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