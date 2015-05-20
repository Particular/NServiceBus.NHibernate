namespace NServiceBus.Unicast.Subscriptions.NHibernate.Installer
{
    using global::NHibernate.Cfg;
    using global::NHibernate.Tool.hbm2ddl;
    using Installation;

    class Installer : INeedToInstallSomething
    {
        public bool RunInstaller { get; set; }
        public Configuration Configuration { get; set; }

        public void Install(string identity, Configure config)
        {
            if (RunInstaller)
            {
                new SchemaUpdate(Configuration).Execute(false, true);
            }
        }
    }
}
