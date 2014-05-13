namespace NServiceBus.UnitOfWork.NHibernate
{
    using System;
    using System.Transactions;
    using Pipeline;
    using Pipeline.Contexts;

    class OpenNativeTransactionBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public IStorageSessionProvider StorageSessionProvider { get; set; }

        public string ConnectionString { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            if (Transaction.Current != null)
            {
                next();
                return;
            }

            using (var transaction = StorageSessionProvider.Session.BeginTransaction())
            {
                context.Set(string.Format("NHibernateTransaction-{0}", ConnectionString), transaction);

                next();

                if (transaction.IsActive)
                {
                    transaction.Commit();
                }
            }
        }
    }
}