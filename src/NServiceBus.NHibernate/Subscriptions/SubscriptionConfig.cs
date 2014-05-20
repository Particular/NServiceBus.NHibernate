namespace NServiceBus.NHibernate
{
    using System;
    using global::NHibernate.Cfg;
    using Persistence;

    public static class SubscriptionConfig
    {
        public static void DisableSubscriptionStorageSchemaUpdate(this PersistenceConfiguration persistenceConfiguration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Subscriptions.AutoUpdateSchema", false);
        }

        public static void UseSubscriptionStorageConfiguration(this PersistenceConfiguration persistenceConfiguration, Configuration configuration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Subscriptions.Configuration", configuration);
        }


        public static void EnableCachingForSubscriptionStorage(this PersistenceConfiguration persistenceConfiguration, TimeSpan expiration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Subscriptions.CacheExpiration", expiration);
        }

    }
}