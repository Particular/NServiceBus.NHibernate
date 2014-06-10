namespace NServiceBus.Persistence.NHibernate
{
    using global::NHibernate.Cfg;
    using global::NHibernate.Tool.hbm2ddl;
    using Installation;

    class Installer : INeedToInstallSomething
    {
        public static bool RunInstaller { get; set; }

        internal static Configuration configuration;

        public void Install(string identity, Configure config)
        {
            if (RunInstaller)
            {
                new SchemaUpdate(configuration).Execute(false, true);
            }
        }
    }
}
