namespace NServiceBus.NHibernate
{
    using global::NHibernate.Cfg;
    using Persistence;
    using Settings;

    public static class SharedConfig
    {
        public static void ConnectionString(this PersistenceConfiguration persistenceConfiguration, string connectionString) 
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Common.ConnectionString", connectionString);
        }

        public static void DisableSchemaUpdate(this PersistenceConfiguration persistenceConfiguration)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Common.AutoUpdateSchema", false);
        }
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
