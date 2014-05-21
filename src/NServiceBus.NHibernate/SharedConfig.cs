namespace NServiceBus.NHibernate
{
    using global::NHibernate.Cfg;
    using Persistence;

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
        public static void ConnectionString(this PersistenceConfiguration persistenceConfiguration, string connectionString) 
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Common.ConnectionString", connectionString);
        }

        /// <summary>
        /// Disables automatic schema update.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        public static void DisableSchemaUpdate(this PersistenceConfiguration persistenceConfiguration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Common.AutoUpdateSchema", false);
        }

        /// <summary>
        /// Configures Subscription Storage to use the <paramref name="configuration"/>.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        public static void UseConfiguration(this PersistenceConfiguration persistenceConfiguration, Configuration configuration)
        {
            persistenceConfiguration.Config.Settings.Set("StorageConfiguration", configuration);
        }

        class Defaults : IWantToRunBeforeConfiguration
        {
            public void Init(Configure configure)
            {
                configure.Settings.SetDefault("NHibernate.Common.AutoUpdateSchema", true);
            }
        }
    }
}
