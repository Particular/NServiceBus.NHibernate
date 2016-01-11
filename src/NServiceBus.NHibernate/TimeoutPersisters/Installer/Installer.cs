namespace NServiceBus.TimeoutPersisters.NHibernate.Installer
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate.Cfg;
    using Installation;

    class Installer : INeedToInstallSomething
    {        
        public ConfigWrapper Configuration { get; set; }

        public Task Install(string identity)
        {
            if (Configuration != null)
            {
                new OptimizedSchemaUpdate(Configuration.Value).Execute(false, true);
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
