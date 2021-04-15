namespace NServiceBus.Features
{
    using System;
    using System.Dynamic;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NHibernate.Tool.hbm2ddl;
    using Microsoft.Extensions.DependencyInjection;
    using Persistence.NHibernate;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Unicast.Subscriptions.NHibernate;
    using Unicast.Subscriptions.NHibernate.Config;
    using Installer = Unicast.Subscriptions.NHibernate.Installer.Installer;

    /// <summary>
    /// NHibernate Subscription Storage
    /// </summary>
    class NHibernateSubscriptionStorage : Feature
    {
        public static readonly string CacheExpirationSettingsKey = "NHibernate.Subscriptions.CacheExpiration";
        public static readonly string AutoupdateschemaSettingsKey = "NHibernate.Subscriptions.AutoUpdateSchema";

        /// <summary>
        /// Creates an instance of <see cref="NHibernateSubscriptionStorage"/>.
        /// </summary>
        public NHibernateSubscriptionStorage()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            DependsOn<MessageDrivenSubscriptions>();
#pragma warning restore CS0618 // Type or member is obsolete

            // since the installers are registered even if the feature isn't enabled we need to make
            // this a no-op of there is no "schema updater" available
            Defaults(c => c.Set(new Installer.SchemaUpdater()));
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            dynamic diagnostics = new ExpandoObject();

            var builder = new NHibernateConfigurationBuilder(context.Settings, diagnostics, "Subscription", "NHibernate.Subscriptions.Configuration");
            builder.AddMappings<SubscriptionMap>();
            var config = builder.Build();

            if (RunInstaller(context))
            {
                context.Settings.Get<Installer.SchemaUpdater>().Execute = identity =>
                {
                    new SchemaUpdate(config.Configuration).Execute(false, true);

                    return Task.FromResult(0);
                };
            }

            var sessionFactory = config.Configuration.BuildSessionFactory();
            SubscriptionPersister persister;
            if (context.Settings.HasSetting(CacheExpirationSettingsKey))
            {
                var cacheFor = context.Settings.Get<TimeSpan>(CacheExpirationSettingsKey);
                persister = new CachedSubscriptionPersister(sessionFactory, cacheFor);

                diagnostics.Cache = true;
                diagnostics.EntriesCashedFor = cacheFor;
            }
            else
            {
                persister = new SubscriptionPersister(sessionFactory);
                diagnostics.Cache = false;
            }

            context.Services.AddSingleton(persister);
            context.Services.AddSingleton<ISubscriptionStorage>(sp => sp.GetRequiredService<SubscriptionPersister>());
            context.RegisterStartupTask(new SubscriptionPersisterInitTask(persister));

            context.Settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.NHibernate.Subscriptions", (object)diagnostics);
        }

        static bool RunInstaller(FeatureConfigurationContext context)
        {
            return context.Settings.Get<bool>(context.Settings.HasSetting(AutoupdateschemaSettingsKey)
                ? AutoupdateschemaSettingsKey
                : "NHibernate.Common.AutoUpdateSchema");
        }

        class SubscriptionPersisterInitTask : FeatureStartupTask
        {
            SubscriptionPersister persister;

            public SubscriptionPersisterInitTask(SubscriptionPersister persister)
            {
                this.persister = persister;
            }

            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                return persister.Init(cancellationToken);
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}
