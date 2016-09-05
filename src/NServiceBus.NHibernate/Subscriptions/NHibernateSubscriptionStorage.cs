namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate.Tool.hbm2ddl;
    using Persistence.NHibernate;
    using Unicast.Subscriptions.NHibernate;
    using Unicast.Subscriptions.NHibernate.Config;
    using Unicast.Subscriptions.NHibernate.Installer;

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
            DependsOn<MessageDrivenSubscriptions>();

            // since the installers are registered even if the feature isn't enabled we need to make
            // this a no-op of there is no "schema updater" available
            Defaults(c => c.Set<Installer.SchemaUpdater>(new Installer.SchemaUpdater()));
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var builder = new NHibernateConfigurationBuilder(context.Settings, "Subscription", "NHibernate.Subscriptions.Configuration", "StorageConfiguration");
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
            if (context.Settings.HasSetting(CacheExpirationSettingsKey))
            {
                context.Container.ConfigureComponent(b => new CachedSubscriptionPersister(sessionFactory, context.Settings.Get<TimeSpan>(CacheExpirationSettingsKey)), DependencyLifecycle.SingleInstance);
            }
            else
            {
                context.Container.ConfigureComponent(b => new SubscriptionPersister(sessionFactory), DependencyLifecycle.SingleInstance);
            }
        }

        static bool RunInstaller(FeatureConfigurationContext context)
        {
            return context.Settings.Get<bool>(context.Settings.HasSetting(AutoupdateschemaSettingsKey)
                ? AutoupdateschemaSettingsKey
                : "NHibernate.Common.AutoUpdateSchema");
        }
    }
}