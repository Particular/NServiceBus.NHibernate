namespace NServiceBus.UnitOfWork.NHibernate
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Transactions;
    using global::NHibernate;
    using global::NHibernate.Impl;
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

        public void Dispose()
        {
            // Injected
        }

        void IManageUnitsOfWork.Begin()
        {
            session.Value = null;
            connection.Value = null;
        }

        void IManageUnitsOfWork.End(Exception ex)
        {
            if (session.Value == null)
            {
                return;
            }

            try
            {
                using (session.Value)
                using (session.Value.Transaction)
                {
                    if (!session.Value.Transaction.IsActive)
                    {
                        return;
                    }

                    if (ex != null)
                    {
                        // Due to a race condition in NH3.3, explicit rollback can cause exceptions and corrupt the connection pool. 
                        // Especially if there are more than one NH session taking part in the DTC transaction
                        //currentSession.Transaction.Rollback();
                    }
                    else
                    {
                        session.Value.Transaction.Commit();
                    }
                }
            }
            finally
            {
                if (connection.Value != null)
                {
                    connection.Value.Dispose();
                }
            }
        }

        internal ISession GetCurrentSession()
        {
            if (session.Value == null)
            {
                var sessionFactoryImpl = SessionFactory as SessionFactoryImpl;

                if (sessionFactoryImpl != null)
                {
                    connection.Value = sessionFactoryImpl.ConnectionProvider.GetConnection();
                    session.Value = SessionFactory.OpenSession(connection.Value);
                }
                else
                {
                    session.Value = SessionFactory.OpenSession();
                }

                session.Value.BeginTransaction(GetIsolationLevel());
            }

            return session.Value;
        }

        IsolationLevel GetIsolationLevel()
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

        ThreadLocal<IDbConnection> connection = new ThreadLocal<IDbConnection>();
        ThreadLocal<ISession> session = new ThreadLocal<ISession>();
    }
}