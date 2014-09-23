namespace NServiceBus.TimeoutPersisters.NHibernate.Installer
{
    using global::NHibernate.Cfg;
    using Installation;

    class Installer : INeedToInstallSomething
    {
        public static bool RunInstaller { get; set; }

        internal static Configuration configuration;

        public void Install(string identity, Configure config)
        {
            if (RunInstaller)
            {
                new OptimizedSchemaUpdate(configuration).Execute(false, true);
            }
        }
    }
}
