namespace NServiceBus.TimeoutPersisters.NHibernate.Installer
{
    using System;
    using System.Threading.Tasks;
    using Installation;
    using Settings;

    class Installer : INeedToInstallSomething
    {
        public Installer(ReadOnlySettings settings)
        {
            settings.TryGet(out schemaUpdater);
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
            public SchemaUpdater()
            {
                Execute = _ => Task.FromResult(0);
            }

            public Func<string, Task> Execute { get; set; }
        }
    }
}
