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
                s.EnableFeatureByDefault<NHibernateStorageSession>();
            });

#pragma warning disable CS0618 // Type or member is obsolete
            Supports<StorageType.GatewayDeduplication>(s => s.EnableFeatureByDefault<NHibernateGatewayDeduplication>());
#pragma warning restore CS0618 // Type or member is obsolete
            Supports<StorageType.Timeouts>(s => s.EnableFeatureByDefault<NHibernateTimeoutStorage>());
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<NHibernateSagaStorage>());
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<NHibernateSubscriptionStorage>());
            Supports<StorageType.Outbox>(s => { });
        }
    }
}