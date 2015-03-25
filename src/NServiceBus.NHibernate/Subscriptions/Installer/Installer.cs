namespace NServiceBus.Unicast.Subscriptions.NHibernate.Installer
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate.Cfg;
    using global::NHibernate.Tool.hbm2ddl;
    using Installation;

    class Installer : INeedToInstallSomething
    {
        public ConfigWrapper Configuration { get; set; }

        public Task Install(string identity)
        {
            if (Configuration != null)
            {
                new SchemaUpdate(Configuration.Value).Execute(false, true);
            }
            return Task.FromResult(0);
        }

        public class ConfigWrapper
        {
            public Configuration Value { get; }

            public ConfigWrapper(Configuration value)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                Value = value;
            }
        }
    }
}
