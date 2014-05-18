namespace NServiceBus.NHibernate.SharedSession
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

        public class Registration : RegisterBehavior
        {
            public Registration()
                : base("OpenNHibernateTransaction", typeof(OpenNativeTransactionBehavior), "Makes sure that there is a NHibernate Transaction wrapping the pipeline")
            {
                InsertAfter("OpenNHibernateSession");
            }
        }
    }
}