namespace NServiceBus.NHibernate.Internal
{
    using System;
    using System.Transactions;
    using global::NHibernate;
    using Janitor;
    using IsolationLevel = System.Data.IsolationLevel;

    [SkipWeaving]
    class AmbientTransactionAwareWrapper : IDisposable
    {
        readonly ISession session;
        ITransaction transaction;

        public AmbientTransactionAwareWrapper(ISession session, IsolationLevel isolationLevel)
        {
            this.session = session;
            session.FlushMode = FlushMode.Never;

            if (Transaction.Current == null)
            {
                transaction = session.BeginTransaction(isolationLevel);
            }
        }

        public AmbientTransactionAwareWrapper(IStatelessSession session, IsolationLevel isolationLevel)
        {
            if (Transaction.Current == null)
            {
                transaction = session.BeginTransaction(isolationLevel);
            }
        }

        public void Dispose()
        {
            if (transaction != null)
            {
                transaction.Dispose();
            }
        }

        public void Commit()
        {
            if (session != null)
            {
                session.Flush();
            }

            if (transaction == null)
            {
                return;
            }

            if (!transaction.IsActive)
            {
                return;
            }

            transaction.Commit();
        }

        public void Rollback()
        {
            if (transaction == null)
            {
                return;
            }

            if (!transaction.IsActive)
            {
                return;
            }

            transaction.Rollback();
        }
    }
}