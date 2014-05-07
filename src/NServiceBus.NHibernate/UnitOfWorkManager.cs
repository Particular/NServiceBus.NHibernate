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
                var session = context.Value.Get<ISession>();

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

                    using (var transaction = context.Value.Get<ITransaction>())
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

                    context.Value.Remove<ITransaction>();
                }
                finally
                {
                    if (!context.Value.Get<bool>("NHibernate.UnitOfWorkManager.OutsideSession"))
                    {
                        session.Dispose();
                        context.Value.Remove<ISession>();
                    }
                }
            }
            finally
            {
                if (!context.Value.Get<bool>("NHibernate.UnitOfWorkManager.OutsideConnection"))
                {
                    context.Value.Get<IDbConnection>().Dispose();
                    context.Value.Remove<IDbConnection>();
                }
            }
        }

        internal ISession GetCurrentSession()
        {
            ISession sessiondb;

            if (getCurrentSessionInitialized.Value)
            {
                sessiondb = context.Value.Get<ISession>();
            }
            else
            {
                IDbConnection dbConnection;
                if (context.Value.TryGet(out dbConnection))
                {
                    context.Value.Set("NHibernate.UnitOfWorkManager.OutsideConnection", true);
                }
                else
                {
                    dbConnection = SessionFactory.GetConnection();
                    context.Value.Set(typeof(IDbConnection).FullName, dbConnection);
                }

                if (context.Value.TryGet(out sessiondb))
                {
                    context.Value.Set("NHibernate.UnitOfWorkManager.OutsideSession", true);
                }
                else
                {
                    sessiondb = SessionFactory.OpenSessionEx(dbConnection);
                    context.Value.Set(typeof(ISession).FullName, sessiondb);
                }

                sessiondb.FlushMode = FlushMode.Never;

                if (Transaction.Current == null)
                {
                    ITransaction transaction;
                    if (context.Value.TryGet(out transaction))
                    {
                        context.Value.Set("NHibernate.UnitOfWorkManager.OutsideTransaction", true);
                    }
                    else
                    {
                        transaction = sessiondb.BeginTransaction(GetIsolationLevel());
                        context.Value.Set(typeof(ITransaction).FullName, transaction);
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