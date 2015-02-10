namespace NServiceBus.Features
{
    using System;

    class TransportIntegrationFeature : Feature
    {
        public TransportIntegrationFeature()
        {
            EnableByDefault();
            DependsOn<NHibernateStorageSession>();
            DependsOn("SqlServerTransport");
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.GetOrDefault<bool>("Transactions.SuppressDistributedTransactions"))
            {
                throw new InvalidOperationException(@"In order for NHibernate persistence to work with SQLServer transport, ambient transactions need to be enabled. 
Do not use busConfig.DisableDistributedTransactions(). 
Fear not, for transaction WILL NOT be escalated to a distrubuted transaction because SQLServer ADO.NET driver supports promotable enlistements and both NHibernate persistence and SQLServer transport will use the same connection.");
            }
        }
    }
}