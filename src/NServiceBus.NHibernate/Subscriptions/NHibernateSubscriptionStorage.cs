namespace NServiceBus.Features
{
    using System;
    using NHibernate.Cfg;
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
            var configuration = context.Settings.GetOrDefault<Configuration>("NHibernate.Subscriptions.Configuration") ?? context.Settings.GetOrDefault<Configuration>("StorageConfiguration");

            if (configuration == null)
            {
                var properties = new ConfigureNHibernate(context.Settings).SubscriptionStorageProperties;
                configuration = new Configuration().SetProperties(properties);
            }
            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(configuration.Properties);

            ConfigureNHibernate.AddMappings<SubscriptionMap>(configuration);

            context.Container.ConfigureComponent<Unicast.Subscriptions.NHibernate.Installer.Installer>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.Configuration, configuration)
                .ConfigureProperty(x => x.RunInstaller, RunInstaller(context));

            var sessionSource = new SubscriptionStorageSessionProvider(configuration.BuildSessionFactory());

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