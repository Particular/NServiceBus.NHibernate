namespace NServiceBus.Deduplication.NHibernate.Installer
{
    using global::NHibernate.Cfg;
    using NServiceBus.Installation;
    using NServiceBus.TimeoutPersisters.NHibernate.Installer;

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