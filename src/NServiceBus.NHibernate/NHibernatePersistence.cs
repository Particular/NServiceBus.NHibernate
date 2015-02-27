namespace NServiceBus
{
    using Features;
    using Persistence;

    /// <summary>
    /// NHibernate persistence for everything.
    /// </summary>
    public class NHibernatePersistence : PersistenceDefinition
    {
        /// <summary>
        /// Constructor that defines the capabilities of the storage
        /// </summary>
        public NHibernatePersistence()
        {
            Defaults(s =>
            {
                s.SetDefault("NHibernate.Common.AutoUpdateSchema", true);

                //we can always enable these ones since they will only enable if the outbox or sagas are on
                s.EnableFeatureByDefault<NHibernateDBConnectionProvider>();
                s.EnableFeatureByDefault<NHibernateStorageSession>();
            });
           
            Supports(Storage.GatewayDeduplication, s => s.EnableFeatureByDefault<NHibernateGatewayDeduplication>());
            Supports(Storage.Timeouts, s => s.EnableFeatureByDefault<NHibernateTimeoutStorage>());
            Supports(Storage.Sagas, s => s.EnableFeatureByDefault<NHibernateSagaStorage>());
            Supports(Storage.Subscriptions, s => s.EnableFeatureByDefault<NHibernateSubscriptionStorage>());
            Supports(Storage.Outbox, s => s.EnableFeatureByDefault<NHibernateOutboxStorage>());
        }
    }
}