namespace NServiceBus.UnitOfWork.NHibernate
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Transactions;
    using global::NHibernate;
    using Persistence.NHibernate;
    using Pipeline;
    using IsolationLevel = System.Data.IsolationLevel;

    /// <summary>
    ///     Implementation of unit of work management with NHibernate
    /// </summary>
    public class UnitOfWorkManager : IManageUnitsOfWork, IDisposable
    {
        /// <summary>
        ///     Injected NHibernate session factory.
        /// </summary>
        public ISessionFactory SessionFactory { get; set; }
        public PipelineExecutor PipelineExecutor { get; set; }

        public string ConnectionString { get; set; }
        
        public void Dispose()
        {
            // Injected
        }

        void IManageUnitsOfWork.Begin()
        {
            getCurrentSessionInitialized.Value = false;
            context.Value = PipelineExecutor.CurrentContext;
        }

        void IManageUnitsOfWork.End(Exception ex)
        {
            if (!getCurrentSessionInitialized.Value)
            {
                return;
            }

            try
            {
                var session = context.Value.Get<ISession>(string.Format("NHibernateSession-{0}", ConnectionString));

                try
                {
                    if (ex == null)
                    {
                        session.Flush();
                    }

                    if (Transaction.Current != null)
                    {
                        return;
                    }

                    if (context.Value.Get<bool>("NHibernate.UnitOfWorkManager.OutsideTransaction"))
                    {
                        return;
                    }

                    using (var transaction = context.Value.Get<ITransaction>(string.Format("NHibernateTransaction-{0}", ConnectionString)))
                    {
                        if (!transaction.IsActive)
                        {
                            return;
                        }

                        // Due to a race condition in NH3.3, explicit rollback can cause exceptions and corrupt the connection pool. 
                        // Especially if there are more than one NH session taking part in the DTC transaction.
                        // Hence the reason we do not call transaction.Rollback() in this code.

                        if (ex == null)
                        {
                            transaction.Commit();
                        }
                    }

                    context.Value.Remove(string.Format("NHibernateTransaction-{0}", ConnectionString));
                }
                finally
                {
                    if (!context.Value.Get<bool>("NHibernate.UnitOfWorkManager.OutsideSession"))
                    {
                        session.Dispose();
                        context.Value.Remove(string.Format("NHibernateSession-{0}", ConnectionString));
                    }
                }
            }
            finally
            {
                if (!context.Value.Get<bool>("NHibernate.UnitOfWorkManager.OutsideConnection"))
                {
                    var connectionKey = string.Format("SqlConnection-{0}", ConnectionString);
                    context.Value.Get<IDbConnection>(connectionKey).Dispose();
                    context.Value.Remove(connectionKey);
                }
            }
        }

        internal ISession GetCurrentSession()
        {
            ISession sessiondb;

            if (getCurrentSessionInitialized.Value)
            {
                sessiondb = context.Value.Get<ISession>(string.Format("NHibernateSession-{0}", ConnectionString));
            }
            else
            {
                IDbConnection dbConnection;
                if (context.Value.TryGet(string.Format("SqlConnection-{0}", ConnectionString), out dbConnection))
                {
                    context.Value.Set("NHibernate.UnitOfWorkManager.OutsideConnection", true);
                }
                else
                {
                    dbConnection = SessionFactory.GetConnection();
                    context.Value.Set(string.Format("SqlConnection-{0}", ConnectionString), dbConnection);
                }

                if (context.Value.TryGet(string.Format("NHibernateSession-{0}", ConnectionString), out sessiondb))
                {
                    context.Value.Set("NHibernate.UnitOfWorkManager.OutsideSession", true);
                }
                else
                {
                    sessiondb = SessionFactory.OpenSessionEx(dbConnection);
                    context.Value.Set(string.Format("NHibernateSession-{0}", ConnectionString), sessiondb);
                }

                sessiondb.FlushMode = FlushMode.Never;

                if (Transaction.Current == null)
                {
                    ITransaction transaction;
                    if (context.Value.TryGet(string.Format("NHibernateTransaction-{0}", ConnectionString), out transaction))
                    {
                        context.Value.Set("NHibernate.UnitOfWorkManager.OutsideTransaction", true);
                    }
                    else
                    {
                        transaction = sessiondb.BeginTransaction(GetIsolationLevel());
                        context.Value.Set(string.Format("NHibernateTransaction-{0}", ConnectionString), transaction);
                    }
                }

                getCurrentSessionInitialized.Value = true;
            }

            return sessiondb;
        }

        static IsolationLevel GetIsolationLevel()
        {
            if (Transaction.Current == null)
            {
                return IsolationLevel.Unspecified;
            }

            switch (Transaction.Current.IsolationLevel)
            {
                case System.Transactions.IsolationLevel.Chaos:
                    return IsolationLevel.Chaos;
                case System.Transactions.IsolationLevel.ReadCommitted:
                    return IsolationLevel.ReadCommitted;
                case System.Transactions.IsolationLevel.ReadUncommitted:
                    return IsolationLevel.ReadUncommitted;
                case System.Transactions.IsolationLevel.RepeatableRead:
                    return IsolationLevel.RepeatableRead;
                case System.Transactions.IsolationLevel.Serializable:
                    return IsolationLevel.Serializable;
                case System.Transactions.IsolationLevel.Snapshot:
                    return IsolationLevel.Snapshot;
                case System.Transactions.IsolationLevel.Unspecified:
                    return IsolationLevel.Unspecified;
                default:
                    return IsolationLevel.Unspecified;
            }
        }

        ThreadLocal<bool> getCurrentSessionInitialized = new ThreadLocal<bool>();
        ThreadLocal<BehaviorContext> context = new ThreadLocal<BehaviorContext>();
    }
}