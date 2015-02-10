namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data;
    using NServiceBus.Settings;
    using Pipeline;
    using Pipeline.Contexts;

    class OpenSqlConnectionBehavior : IBehavior<IncomingContext>
    {
        public SessionFactoryProvider SessionFactoryProvider { get; set; }
        public ReadOnlySettings Settings { get; set; }

        public string ConnectionString { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            IDbConnection existingConnection;

            if (context.TryGet(string.Format("SqlConnection-{0}", ConnectionString), out existingConnection))
            {
                var transactionScopeDisabled = Settings.Get<bool>("Transactions.SuppressDistributedTransactions");
                if (transactionScopeDisabled)
                {
                    throw new InvalidOperationException(@"In order for NHibernate persistence to work with SQLServer transport, ambient transactions need to be enabled. 
Do not use busConfig.DisableDistributedTransactions(). 
Fear not, the transaction WILL NOT be escalated to a distrubuted transaction because SQLServer ADO.NET driver supports promotable enlistements and both NHibernate persistence and SQLServer transport will use the same connection.");
                }
                next();
                return;
            }

            var lazyConnection = new Lazy<IDbConnection>(() => SessionFactoryProvider.SessionFactory.GetConnection());

            context.Set(string.Format("LazySqlConnection-{0}", ConnectionString), lazyConnection);
            try
            {
                next();
            }
            finally
            {
                if (lazyConnection.IsValueCreated)
                {
                    lazyConnection.Value.Dispose();
                }

                context.Remove(string.Format("LazySqlConnection-{0}", ConnectionString));
            }
        }

        public class Registration : RegisterStep
        {
            public Registration(): base("OpenSqlConnection", typeof(OpenSqlConnectionBehavior), "Makes sure that there is an IDbConnection available on the pipeline")
            {
                InsertAfter(WellKnownStep.CreateChildContainer);
                InsertBeforeIfExists("OutboxDeduplication");
                InsertBefore(WellKnownStep.ExecuteUnitOfWork);
            }
        }
    }
}