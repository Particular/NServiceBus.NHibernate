namespace NServiceBus.NHibernate
{
    using System;
    using global::NHibernate.Cfg;
    using Persistence;

    /// <summary>
    /// Subscription configuration extensions.
    /// </summary>
    public static class SubscriptionConfig
    {
        /// <summary>
        /// Disables automatic schema update.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        public static void DisableSubscriptionStorageSchemaUpdate(this PersistenceConfiguration persistenceConfiguration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Subscriptions.AutoUpdateSchema", false);
        }

        /// <summary>
        /// Configures Subscription Storage to use the <paramref name="configuration"/>.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        public static void UseSubscriptionStorageConfiguration(this PersistenceConfiguration persistenceConfiguration, Configuration configuration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Subscriptions.Configuration", configuration);
        }


        /// <summary>
        /// Enables Subscription Storage to use caching.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="expiration">The period of time to cache subscriptions list for.</param>
        public static void EnableCachingForSubscriptionStorage(this PersistenceConfiguration persistenceConfiguration, TimeSpan expiration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Subscriptions.CacheExpiration", expiration);
        }

    }
}