namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Linq;
    using System.Text;
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
                var schemaUpdate = new SchemaUpdate(Configuration);
                var sb = new StringBuilder();
                schemaUpdate.Execute(s => sb.AppendLine(s), true);

                if (!schemaUpdate.Exceptions.Any())
                {
                    return;
                }

                var aggregate = new AggregateException(schemaUpdate.Exceptions);

                var errorMessage = @"Schema update failed.
The following exception(s) were thrown:
{0}

TSql Script:
{1}";
                throw new Exception(String.Format(errorMessage, aggregate, sb));
            }
        }
    }
}
