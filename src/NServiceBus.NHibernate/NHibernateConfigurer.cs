namespace NServiceBus.Persistence
{
    using System.Collections.Generic;
    using Features;

    class NHibernateConfigurer : IConfigurePersistence<NServiceBus.NHibernate>
    {

        public void Enable(Configure config, List<Storage> storagesToEnable)
        {
            config.Settings.SetDefault("NHibernate.Common.AutoUpdateSchema", true);

            //we can always enable these ones since they will only enable if the outbox or sagas are on
            config.Settings.EnableFeatureByDefault<NHibernateDBConnectionProvider>();
            config.Settings.EnableFeatureByDefault<NHibernateStorageSession>();

            if (storagesToEnable.Contains(Storage.Outbox))
                config.Settings.EnableFeatureByDefault<NHibernateOutboxStorage>();

            if (storagesToEnable.Contains(Storage.Sagas))
                config.Settings.EnableFeatureByDefault<NHibernateSagaStorage>();

            if (storagesToEnable.Contains(Storage.Subscriptions))
                config.Settings.EnableFeatureByDefault<NHibernateSubscriptionStorage>();

            if (storagesToEnable.Contains(Storage.Timeouts))
                config.Settings.EnableFeatureByDefault<NHibernateTimeoutStorage>();

            if (storagesToEnable.Contains(Storage.GatewayDeduplication))
                config.Settings.EnableFeatureByDefault<NHibernateGatewayDeduplication>();
        }
    }
}