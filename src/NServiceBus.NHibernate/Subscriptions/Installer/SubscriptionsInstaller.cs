namespace NServiceBus.Unicast.Subscriptions.NHibernate.Installer
{
    using System.Threading;
    using System.Threading.Tasks;
    using global::NHibernate.Cfg;
    using global::NHibernate.Tool.hbm2ddl;
    using Installation;

    class SubscriptionsInstaller(SubscriptionNHibernateConfiguration config) : INeedToInstallSomething
    {
        public Task Install(string identity, CancellationToken cancellationToken = default)
        {
            new SchemaUpdate(config.Config).Execute(false, true);
            return Task.CompletedTask;
        }
    }

    class SubscriptionNHibernateConfiguration(Configuration config)
    {
        public Configuration Config => config;
    }
}
