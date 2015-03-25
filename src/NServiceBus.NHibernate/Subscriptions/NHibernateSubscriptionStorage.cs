namespace NServiceBus.Features
{
    using System;
    using Persistence.NHibernate;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
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
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var builder = new NHibernateConfigurationBuilder(context.Settings, "Subscription", "NHibernate.Subscriptions.Configuration", "StorageConfiguration");
            builder.AddMappings<SubscriptionMap>();
            var config = builder.Build();

            context.Container.ConfigureComponent<Installer>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.Configuration, RunInstaller(context) ? new Installer.ConfigWrapper(config.Configuration) : null);

            var sessionFactory = config.Configuration.BuildSessionFactory();
            if (context.Settings.HasSetting(CacheExpirationSettingsKey))
            {
                var persister = new CachedSubscriptionPersister(sessionFactory, context.Settings.Get<TimeSpan>(CacheExpirationSettingsKey));
                context.Container.RegisterSingleton<ISubscriptionStorage>(persister);
            }
            else
            {
                var persister = new SubscriptionPersister(sessionFactory);
                context.Container.RegisterSingleton<ISubscriptionStorage>(persister);
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