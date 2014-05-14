namespace NServiceBus.UnitOfWork.NHibernate
{
    using System;
    using System.Transactions;
    using Pipeline;
    using Pipeline.Contexts;

    class OpenNativeTransactionBehavior : IBehavior<IncomingContext>
    {
        public IStorageSessionProvider StorageSessionProvider { get; set; }

        public string ConnectionString { get; set; }

        public void Invoke(IncomingContext context, Action next)
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