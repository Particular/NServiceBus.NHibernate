namespace NServiceBus.TimeoutPersisters.NHibernate.Installer
{
    using global::NHibernate.Cfg;
    using Installation;

    class Installer : INeedToInstallSomething
    {
        public bool RunInstaller { get; set; }
        public Configuration Configuration { get; set; }

        public void Install(string identity, Configure config)
        {
            if (RunInstaller)
            {
                new OptimizedSchemaUpdate(Configuration).Execute(false, true);
            }
        }
    }
}
