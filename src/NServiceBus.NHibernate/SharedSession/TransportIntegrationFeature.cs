namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Persistence.NHibernate;

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
            if (context.Settings.GetOrDefault<bool>("Transactions.SuppressDistributedTransactions") 
                && context.Container.HasComponent<SharedConnectionStorageSessionProvider>())
            {
                throw new InvalidOperationException(@"In order for NHibernate persistence to work with SQLServer transport, either ambient transactions or outbox need to be enabled. 
In any cases, the transaction WILL NOT be escalated to a distrubuted transaction if transport, NServiceBus persistence and user persistence all use exactly the same connection string.");
            }
        }
    }
}