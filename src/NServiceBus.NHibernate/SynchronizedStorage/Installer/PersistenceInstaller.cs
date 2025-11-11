namespace NServiceBus.Persistence.NHibernate.Installer
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NHibernate.Tool.hbm2ddl;
    using Installation;
    using Settings;

    class PersistenceInstaller(IReadOnlySettings settings) : INeedToInstallSomething
    {
        public Task Install(string identity, CancellationToken cancellationToken = default)
        {
            var config = settings.Get<NHibernateConfiguration>();

            var schemaUpdate = new SchemaUpdate(config.Configuration);
            var sb = new StringBuilder();
            schemaUpdate.Execute(s => sb.AppendLine(s), true);

            if (schemaUpdate.Exceptions.Any())
            {
                var aggregate = new AggregateException(schemaUpdate.Exceptions);

                var errorMessage = @"Schema update failed.
The following exception(s) were thrown:
{0}

TSql Script:
{1}";
                throw new Exception(string.Format(errorMessage, aggregate.Flatten(), sb));
            }

            return Task.CompletedTask;
        }
    }
}
