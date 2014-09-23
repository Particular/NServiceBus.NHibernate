namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using Configuration.AdvanceExtensibility;
    using global::NHibernate.Cfg;

    /// <summary>
    /// Subscription configuration extensions.
    /// </summary>
    public static class SubscriptionConfig
    {
        /// <summary>
        /// Disables automatic schema update.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        public static PersistenceExtentions<NHibernatePersistence> DisableSubscriptionStorageSchemaUpdate(this PersistenceExtentions<NHibernatePersistence> persistenceConfiguration)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.Subscriptions.AutoUpdateSchema", false);
            return persistenceConfiguration;
        }

        /// <summary>
        /// Configures Subscription Storage to use the <paramref name="configuration"/>.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        public static PersistenceExtentions<NHibernatePersistence> UseSubscriptionStorageConfiguration(this PersistenceExtentions<NHibernatePersistence> persistenceConfiguration, Configuration configuration)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.Subscriptions.Configuration", configuration);
            return persistenceConfiguration;
        }


        /// <summary>
        /// Enables Subscription Storage to use caching.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="expiration">The period of time to cache subscriptions list for.</param>
        public static PersistenceExtentions<NHibernatePersistence> EnableCachingForSubscriptionStorage(this PersistenceExtentions<NHibernatePersistence> persistenceConfiguration, TimeSpan expiration)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.Subscriptions.CacheExpiration", expiration);
            return persistenceConfiguration;
        }
    }
}