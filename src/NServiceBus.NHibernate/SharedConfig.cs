namespace NServiceBus.NHibernate
{
    using global::NHibernate.Cfg;
    using Persistence;
    using Settings;

    public static class SharedConfig
    {
        public static void ConnectionString(this PersistenceConfiguration config, string connectionString) 
        {
            SettingsHolder.Instance.Set("NHibernate.Common.ConnectionString",connectionString);
        }

        public static void DisableSchemaUpdate(this PersistenceConfiguration config)
        {
            SettingsHolder.Instance.Set("NHibernate.Common.AutoUpdateSchema", false);
        }
        public static void UseConfiguration(this PersistenceConfiguration config, Configuration configuration)
        {
            SettingsHolder.Instance.Set("StorageConfiguration", configuration);
        }



        class Defaults : ISetDefaultSettings
        {
            public Defaults()
            {
                SettingsHolder.Instance.SetDefault("NHibernate.Common.AutoUpdateSchema", true);
            }
        }
    }
}
