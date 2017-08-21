namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using global::NHibernate.Cfg;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Features;

    /// <summary>
    /// Subscription configuration extensions.
    /// </summary>
    public static class SubscriptionConfig
    {
        /// <summary>
        /// Disables automatic schema update.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        public static PersistenceExtensions<NHibernatePersistence> DisableSubscriptionStorageSchemaUpdate(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration)
        {
            persistenceConfiguration.GetSettings().Set(NHibernateSubscriptionStorage.AutoupdateschemaSettingsKey, false);
            return persistenceConfiguration;
        }

        /// <summary>
        /// Configures Subscription Storage to use the <paramref name="configuration"/>.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        public static PersistenceExtensions<NHibernatePersistence> UseSubscriptionStorageConfiguration(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration, Configuration configuration)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.Subscriptions.Configuration", configuration);
            return persistenceConfiguration;
        }


        /// <summary>
        /// Enables Subscription Storage to use caching.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="expiration">The period of time to cache subscriptions list for.</param>
        public static PersistenceExtensions<NHibernatePersistence> EnableCachingForSubscriptionStorage(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration, TimeSpan expiration)
        {
            persistenceConfiguration.GetSettings().Set(NHibernateSubscriptionStorage.CacheExpirationSettingsKey, expiration);
            return persistenceConfiguration;
        }
    }
}