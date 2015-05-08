namespace NServiceBus.Persistence
{
    using System;
    using Configuration.AdvanceExtensibility;
    using global::NHibernate;
    using global::NHibernate.Cfg;

    /// <summary>
    /// Shared configuration extensions.
    /// </summary>
    public static class SharedConfig
    {
        /// <summary>
        /// Sets the connection string to use for all storages
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="connectionString">The connection string to use.</param>
        public static PersistenceExtentions<NHibernatePersistence> ConnectionString(this PersistenceExtentions<NHibernatePersistence> persistenceConfiguration, string connectionString) 
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.Common.ConnectionString", connectionString);
            return persistenceConfiguration;
        }

        /// <summary>
        /// Disables automatic schema update.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        public static PersistenceExtentions<NHibernatePersistence> DisableSchemaUpdate(this PersistenceExtentions<NHibernatePersistence> persistenceConfiguration)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.Common.AutoUpdateSchema", false);
            return persistenceConfiguration;
        }

        /// <summary>
        /// Configures Subscription Storage to use the <paramref name="configuration"/>.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        public static PersistenceExtentions<NHibernatePersistence> UseConfiguration(this PersistenceExtentions<NHibernatePersistence> persistenceConfiguration, Configuration configuration)
        {
            persistenceConfiguration.GetSettings().Set("StorageConfiguration", configuration);
            return persistenceConfiguration;
        }

        /// <summary>
        /// Instructs the NHibernate persistence to register the managed session available via NHibernateStorageSession in the container.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <returns></returns>
        public static PersistenceExtentions<NHibernatePersistence> RegisterManagedSessionInTheContainer(this PersistenceExtentions<NHibernatePersistence> persistenceConfiguration)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.RegisterManagedSession", true);
            return persistenceConfiguration;
        }

        /// <summary>
        /// Instructs the NHibernate persistence to use a custom session creation method. The provided method takes the ISessionFactory and the connection string and returns a session.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static PersistenceExtentions<NHibernatePersistence> UseCustomSessionCreationMethod(this PersistenceExtentions<NHibernatePersistence> persistenceConfiguration, Func<ISessionFactory, string, ISession> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }
            persistenceConfiguration.GetSettings().Set("NHibernate.SessionCreator", callback);
            return persistenceConfiguration;
        }
    }
}
