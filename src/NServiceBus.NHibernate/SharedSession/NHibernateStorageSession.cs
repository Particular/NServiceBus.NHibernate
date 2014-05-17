namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using NHibernate.SharedSession;
    using global::NHibernate.Cfg;
    using global::NHibernate;
    using Environment = global::NHibernate.Cfg.Environment;
    using ObjectBuilder;
    using Pipeline;
    using Pipeline.Contexts;
    using Settings;
    using UnitOfWork;

    public class NHibernateStorageSession : Feature
    {
        public override bool ShouldBeEnabled()
        {
            return IsEnabled<NHibernateSagaStorage>() || IsEnabled<NHibernateOutboxStorage>();
        }

        public override void Initialize(Configure config)
        {
            var configuration = config.Settings.GetOrDefault<Configuration>("StorageConfiguration");

            if (configuration == null)
            {
                var properties = config.Settings.Get<IDictionary<string, string>>("StorageProperties");

                configuration = new Configuration().SetProperties(properties);

                foreach (var modification in config.Settings.Get<List<Action<Configuration>>>("StorageConfigurationModifications"))
                {
                    modification(configuration);
                }
            }

            string connString;

            if (!configuration.Properties.TryGetValue(Environment.ConnectionString, out connString))
            {
                string connStringName;

                if (configuration.Properties.TryGetValue(Environment.ConnectionStringName, out connStringName))
                {

                    var connectionStringSettings = System.Configuration.ConfigurationManager.ConnectionStrings[connStringName];

                    connString = connectionStringSettings.ConnectionString;
                }
            }

            config.Configurer.RegisterSingleton<ISessionFactory>(configuration.BuildSessionFactory());

            config.Configurer.ConfigureComponent<StorageSessionProvider>(DependencyLifecycle.InstancePerCall)
                     .ConfigureProperty(p => p.ConnectionString, connString);

            config.Configurer.ConfigureComponent<DbConnectionProvider>(DependencyLifecycle.InstancePerCall)
                     .ConfigureProperty(p => p.ConnectionString, connString);

            config.Configurer.ConfigureComponent<OpenSqlConnectionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.ConnectionString, connString);

            config.Configurer.ConfigureComponent<OpenSessionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.ConnectionString, connString);

            config.Configurer.ConfigureComponent<OpenNativeTransactionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.ConnectionString, connString);

            Installer.RunInstaller = config.Settings.Get<bool>("NHibernate.Common.AutoUpdateSchema");

            Installer.configuration = configuration;
        }

        class Defaults : ISetDefaultSettings
        {
            public Defaults()
            {
                SettingsHolder.Instance.SetDefault("AutoUpdateSchema", true);
                SettingsHolder.Instance.SetDefault("StorageProperties", new Dictionary<string, string>());
                SettingsHolder.Instance.SetDefault("StorageConfigurationModifications", new List<Action<Configuration>>());
            }
        }

        class PipelineConfig : PipelineOverride
        {
            public override void Override(BehaviorList<IncomingContext> behaviorList)
            {
                if (!IsEnabled<NHibernateStorageSession>())
                {
                    return;
                }

                //this one needs to go first to make sure that the outbox have a connection
                behaviorList.InnerList.Insert(0, typeof(OpenSqlConnectionBehavior));

                behaviorList.InsertBefore<UnitOfWorkBehavior, OpenSessionBehavior>();


                //we open a native NH tx if needed right after the session has been created
                behaviorList.InsertAfter<OpenSessionBehavior, OpenNativeTransactionBehavior>();
            }
        }
    }
}