namespace NServiceBus.UnitOfWork.NHibernate
{
    using System;
    using System.Transactions;
    using Pipeline;
    using Pipeline.Contexts;
    using IsolationLevel = System.Data.IsolationLevel;

    class NativeTransactionBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public IStorageSessionProvider StorageSessionProvider { get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }

        public string ConnectionString { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            if (Transaction.Current != null)
            {
                next();
                return;
            }

            using (var transaction = StorageSessionProvider.Session.BeginTransaction(GetIsolationLevel()))
            {
                context.Set(string.Format("NHibernateTransaction-{0}", ConnectionString), transaction);

                next();

                if (transaction.IsActive)
                {
                    transaction.Commit();
                }
            }
        }

        static IsolationLevel GetIsolationLevel()
        {
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
    }
}