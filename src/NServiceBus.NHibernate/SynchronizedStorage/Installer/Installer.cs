namespace NServiceBus.Persistence.NHibernate.Installer
{
    using System;
    using System.Threading.Tasks;
    using Installation;
    using Settings;

    class Installer : INeedToInstallSomething
    {
        public Installer(ReadOnlySettings settings)
        {
            schemaUpdater = settings.Get<SchemaUpdater>();
        }

        public Task Install(string identity)
        {
            return schemaUpdater.Execute(identity);
        }

        SchemaUpdater schemaUpdater;

        public class SchemaUpdater
        {
            public SchemaUpdater()
            {
                Execute = _ => Task.FromResult(0);
            }

            public Func<string, Task> Execute { get; set; }
        }
    }
}
