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
    using Persistence.NHibernate.Installer;


    /// <summary>
    /// NHibernate Storage Session.
    /// </summary>
    public sealed class NHibernateStorageSession : Feature
    {
        /// <summary>
        /// Creates an instance of <see cref="NHibernateStorageSession"/>. This constructor is called by NServiceBus.
        /// </summary>
        public NHibernateStorageSession()
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

            DependsOn<SynchronizedStorage>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var config = context.Settings.Get<NHibernateConfiguration>();

            context.Services.AddSingleton(sb =>
            {
                //This is lazy lazy initialized when first resolving because other features modify configuration in their Setup phases adding their mappings
                var sessionFactory = config.Configuration.BuildSessionFactory();
                return new SessionFactoryHolder(sessionFactory);
            });

            context.Services.AddScoped<ICompletableSynchronizedStorageSession, NHibernateSynchronizedStorageSession>();
            context.Services.AddScoped(sp => (sp.GetService<ICompletableSynchronizedStorageSession>() as INHibernateStorageSessionProvider)?.InternalSession);

            var runInstaller = context.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema");

            if (runInstaller)
            {
                context.AddInstaller<PersistenceInstaller>();
            }

            context.Settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.NHibernate.SynchronizedSession", context.Settings.Get("NServiceBus.NHibernate.NHibernateStorageSessionDiagnostics"));
        }


    }
}