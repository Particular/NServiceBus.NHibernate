namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
// ReSharper disable RedundantNameQualifier
    using global::NHibernate.Cfg;
    using global::NHibernate;
    using Environment = global::NHibernate.Cfg.Environment;
// ReSharper restore RedundantNameQualifier
    using NServiceBus.Outbox.NHibernate;
    using ObjectBuilder;
    using Persistence.NHibernate;
    using Pipeline;
    using Pipeline.Contexts;
    using Settings;
    using UnitOfWork;
    using UnitOfWork.NHibernate;

    public class NHibernateSessionManagement : Feature
    {
        public override bool IsEnabledByDefault
        {
            get { return true; }
        }

        public override bool ShouldBeEnabled()
        {
            return IsEnabled<NHibernateSagaPersistence>() || IsEnabled<NHibernateOutbox>();
        }

        public override void Initialize()
        {
            InitializeInner(Configure.Instance.Configurer);
        }

        void InitializeInner(IConfigureComponents config)
        {
            var configuration = SettingsHolder.GetOrDefault<Configuration>("StorageConfiguration");

            if (configuration == null)
            {
                var properties = SettingsHolder.GetOrDefault<IDictionary<string, string>>("StorageProperties");

                if (properties == null)
                {
                    properties = ConfigureNHibernate.StorageProperties;
                }

                configuration = new Configuration().SetProperties(properties);

                foreach (var modification in SettingsHolder.Get<List<Action<Configuration>>>("StorageConfigurationModifications"))
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

            config.RegisterSingleton<ISessionFactory>(configuration.BuildSessionFactory());

            config.ConfigureComponent<StorageSessionProvider>(DependencyLifecycle.InstancePerCall)
                     .ConfigureProperty(p => p.ConnectionString, connString);

            config.ConfigureComponent<DbConnectionProvider>(DependencyLifecycle.InstancePerCall)
                     .ConfigureProperty(p => p.ConnectionString, connString);

            config.ConfigureComponent<OpenSqlConnectionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.ConnectionString, connString);

            config.ConfigureComponent<OpenSessionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.ConnectionString, connString);

            config.ConfigureComponent<OpenNativeTransactionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(p => p.ConnectionString, connString);

            Installer.RunInstaller = true;

            Installer.configuration = configuration;
        }

        class Defaults : ISetDefaultSettings
        {
            public Defaults()
            {
                SettingsHolder.SetDefault("StorageConfigurationModifications", new List<Action<Configuration>>());
            }
        }

        class PipelineConfig : PipelineOverride
        {
            public override void Override(BehaviorList<IncomingContext> behaviorList)
            {
                if (!IsEnabled<NHibernateSessionManagement>())
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