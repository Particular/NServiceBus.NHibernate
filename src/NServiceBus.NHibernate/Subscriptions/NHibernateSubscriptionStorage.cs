namespace NServiceBus.Features
{
    using System;
    using global::NHibernate.Cfg;
    using NHibernate.Internal;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Unicast.Subscriptions.NHibernate;
    using Unicast.Subscriptions.NHibernate.Config;

    public class NHibernateSubscriptionStorage : Feature
    {
        public NHibernateSubscriptionStorage()
        {
            DependsOn<StorageDrivenPublisher>();
        }

        public override void Initialize(Configure config)
        {
            var properties = new ConfigureNHibernate(config.Settings).SubscriptionStorageProperties;

            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(properties);


            var configuration = config.Settings.GetOrDefault<Configuration>("NHibernate.Subscriptions.Configuration");

            if (configuration == null)
            {
                configuration = new Configuration()
                    .SetProperties(properties);
            }

            ConfigureNHibernate.AddMappings<SubscriptionMap>(configuration);

            
            Unicast.Subscriptions.NHibernate.Installer.Installer.configuration = configuration;


            if (config.Settings.HasSetting("NHibernate.Subscriptions.AutoUpdateSchema"))
            {
                Unicast.Subscriptions.NHibernate.Installer.Installer.RunInstaller = config.Settings.Get<bool>("NHibernate.Subscriptions.AutoUpdateSchema");
            }
            else
            {
                Unicast.Subscriptions.NHibernate.Installer.Installer.RunInstaller = config.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema");
            }
            var sessionSource = new SubscriptionStorageSessionProvider(configuration.BuildSessionFactory());

            config.Configurer.RegisterSingleton<ISubscriptionStorageSessionProvider>(sessionSource);
          
            if (config.Settings.HasSetting("NHibernate.Subscriptions.CacheExpiration"))
            {
                config.Configurer.RegisterSingleton<ISubscriptionStorage>(new CachedSubscriptionStorage(sessionSource, config.Settings.Get<TimeSpan>("NHibernate.Subscriptions.CacheExpiration")));
            }
            else
            {
                config.Configurer.ConfigureComponent<SubscriptionStorage>(DependencyLifecycle.InstancePerCall);
            }
        }
    }
}