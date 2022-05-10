namespace NServiceBus.Features
{
    using System;
    using System.Dynamic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NServiceBus.NHibernate.Outbox;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.Persistence;
    using NServiceBus.Persistence.NHibernate;
    using global::NHibernate.Tool.hbm2ddl;
    using NHibernate.SynchronizedStorage;
    using Installer = Persistence.NHibernate.Installer.Installer;


    /// <summary>
    /// NHibernate Storage Session.
    /// </summary>
    public class NHibernateStorageSession : Feature
    {

        internal NHibernateStorageSession()
        {

            Defaults(s =>
            {
                var diagnosticsObject = new ExpandoObject();
                var builder = new NHibernateConfigurationBuilder(s, diagnosticsObject, "Saga", "StorageConfiguration");
                var config = builder.Build();
                s.SetDefault(config);
                s.SetDefault("NServiceBus.NHibernate.NHibernateStorageSessionDiagnostics", diagnosticsObject);

                s.SetDefault<IOutboxPersisterFactory>(new OutboxPersisterFactory<OutboxRecord>());
            });

            // since the installers are registered even if the feature isn't enabled we need to make
            // this a no-op of there is no "schema updater" available
            Defaults(c => c.Set(new Installer.SchemaUpdater()));
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var config = context.Settings.Get<NHibernateConfiguration>(); //TODO: Should we register it under a Seaga key?

            context.Services.AddSingleton(sb =>
            {
                //This is lazy lazy initialized when first resolving because other features modify configuration in their Setup phases adding their mappings
                var sessionFactory = config.Configuration.BuildSessionFactory();
                return new SessionFactoryHolder(sessionFactory);
            });

            context.Services.AddScoped<ICompletableSynchronizedStorageSession, NHibernateSynchronizedStorageSession>();
            context.Services.AddScoped(sb => sb.GetRequiredService<ICompletableSynchronizedStorageSession>().StorageSession());

            var runInstaller = context.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema");

            if (runInstaller)
            {
                context.Settings.Get<Installer.SchemaUpdater>().Execute = identity =>
                {
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
                    return Task.FromResult(0);
                };
            }


            context.Settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.NHibernate.SynchronizedSession", context.Settings.Get("NServiceBus.NHibernate.NHibernateStorageSessionDiagnostics"));
        }


    }
}
