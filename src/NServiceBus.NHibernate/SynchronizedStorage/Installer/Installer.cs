namespace NServiceBus.Persistence.NHibernate.Installer
{
    using System;
    using System.Threading.Tasks;
    using Installation;
    using NServiceBus.ObjectBuilder;

    class Installer : INeedToInstallSomething
    {
        public Installer(IConfigureComponents configureComponents, IBuilder builder)
        {
            // since the installers are registered even if the feature isn't enabled we need to make 
            // this a no-op of there is no "schema updater" available 
            if (configureComponents.HasComponent<SchemaUpdater>())
            {
                schemaUpdater = builder.Build<SchemaUpdater>();
            }
        }

        public Task Install(string identity)
        {
            if (schemaUpdater == null)
            {
                return Task.FromResult(0);
            }

            return schemaUpdater.Execute(identity);
        }

        SchemaUpdater schemaUpdater;

        public class SchemaUpdater
        {
            public SchemaUpdater(Func<string, Task> execute)
            {
                Execute = execute;
            }

            public Func<string, Task> Execute { get; }
        }
    }
}
