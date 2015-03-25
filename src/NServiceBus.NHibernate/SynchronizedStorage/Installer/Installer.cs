namespace NServiceBus.Persistence.NHibernate.Installer
{
    using System;
    using System.Linq;
    using System.Text;
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
                var schemaUpdate = new SchemaUpdate(Configuration.Value);
                var sb = new StringBuilder();
                schemaUpdate.Execute(s => sb.AppendLine(s), true);

                if (!schemaUpdate.Exceptions.Any())
                {
                    return Task.FromResult(0);
                }

                var aggregate = new AggregateException(schemaUpdate.Exceptions);

                var errorMessage = @"Schema update failed.
The following exception(s) were thrown:
{0}

TSql Script:
{1}";
                throw new Exception(string.Format(errorMessage, aggregate, sb));
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
