namespace NServiceBus.Features
{
    using System;
    using Persistence.NHibernate;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Unicast.Subscriptions.NHibernate;
    using Unicast.Subscriptions.NHibernate.Config;

    /// <summary>
    /// NHibernate Subscription Storage
    /// </summary>
    public class NHibernateSubscriptionStorage : Feature
    {
        /// <summary>
        /// Creates an instance of <see cref="NHibernateSubscriptionStorage"/>.
        /// </summary>
        public NHibernateSubscriptionStorage()
        {
            DependsOn<StorageDrivenPublishing>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var configure = new ConfigureNHibernate(context.Settings, "Subscription", "NHibernate.Subscriptions.Configuration", "StorageConfiguration");
            configure.AddMappings<SubscriptionMap>();

            context.Container.ConfigureComponent<Unicast.Subscriptions.NHibernate.Installer.Installer>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.Configuration, configure.Configuration)
                .ConfigureProperty(x => x.RunInstaller, RunInstaller(context));

            var sessionSource = new SubscriptionStorageSessionProvider(configure.Configuration.BuildSessionFactory());

            context.Container.RegisterSingleton(sessionSource);

            if (context.Settings.HasSetting("NHibernate.Subscriptions.CacheExpiration"))
            {
                context.Container.RegisterSingleton<ISubscriptionStorage>(new CachedSubscriptionPersister(sessionSource, context.Settings.Get<TimeSpan>("NHibernate.Subscriptions.CacheExpiration")));
            }
            else
            {
                context.Container.ConfigureComponent<SubscriptionPersister>(DependencyLifecycle.InstancePerCall);
            }
        }

        static bool RunInstaller(FeatureConfigurationContext context)
        {
            return context.Settings.Get<bool>(context.Settings.HasSetting("NHibernate.Subscriptions.AutoUpdateSchema")
                ? "NHibernate.Subscriptions.AutoUpdateSchema"
                : "NHibernate.Common.AutoUpdateSchema");
        }
    }
}