namespace NServiceBus.NHibernate
{
    using System;
    using global::NHibernate.Cfg;
    using Settings;

    public static class SharedConfig
    {
        public static void ConnectionString(this PersistenceConfiguration config, string connectionString) 
        {
            SettingsHolder.Set("NHibernate.Common.ConnectionString",connectionString);
        }

        public static void DisableSchemaUpdate(this PersistenceConfiguration config)
        {
            SettingsHolder.Set("NHibernate.Common.AutoUpdateSchema", false);
        }
        public static void UseConfiguration(this PersistenceConfiguration config, Configuration configuration)
        {
            SettingsHolder.Set("StorageConfiguration",configuration);
        }



        class Defaults : ISetDefaultSettings
        {
            public Defaults()
            {
                SettingsHolder.SetDefault("NHibernate.Common.AutoUpdateSchema", true);
            }
        }
    }
}
