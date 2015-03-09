namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Outbox;

    /// <summary>
    /// Makes sure that when SQL Server transport is used, either ambient transactions or outbox is enabled.
    /// </summary>
    class ValidateAmbientTransactionOrOutbox : Feature
    {
        public ValidateAmbientTransactionOrOutbox()
        {            
            EnableByDefault();

            //Optional dependency on NHibernateOutboxStorage and a mandatory dependency on SqlServerTransport
            DependsOn("SqlServerTransport");
            DependsOnAtLeastOne("NHibernateOutboxStorage", "SqlServerTransport"); 
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.GetOrDefault<bool>("Transactions.SuppressDistributedTransactions") 
                && !context.Container.HasComponent<OutboxPersister>())
            {
                throw new InvalidOperationException(@"In order for NHibernate persistence to work with SQLServer transport, either ambient transactions or outbox need to be enabled. 
In any case, the transaction WILL NOT be escalated to a distrubuted transaction if transport, NServiceBus persistence and user persistence all use exactly the same connection string.");
            }
        }
    }
}