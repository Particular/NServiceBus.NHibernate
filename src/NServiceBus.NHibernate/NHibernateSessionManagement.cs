namespace NServiceBus.Features
{
    using System.Collections.Generic;
    using System.Linq;
    using global::NHibernate.Cfg;
    using global::NHibernate.Cfg.MappingSchema;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using ObjectBuilder;
    using Persistence.NHibernate;
    using Pipeline;
    using Pipeline.Contexts;
    using Settings;
    using UnitOfWork;
    using UnitOfWork.NHibernate;

    class NHibernateSessionManagement : Feature
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

                foreach (var mapping in SettingsHolder.Get<List<HbmMapping>>("StorageMappings"))
                {
                    configuration.AddMapping(mapping);
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
                SettingsHolder.SetDefault("StorageMappings", new List<HbmMapping>());
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

    public class NHibernateOutbox : Feature
    {
        public override bool IsEnabledByDefault
        {
            get { return true; }
        }

        public override bool ShouldBeEnabled()
        {
            var enabled = IsEnabled<Outbox>();

            if (!enabled)
            {
                return false;
            }

            var mapper = new ModelMapper();
            mapper.AddMapping<OutboxEntityMap>();
            mapper.AddMapping<TransportOperationEntityMap>();

            SettingsHolder.Get<List<HbmMapping>>("StorageMappings")
                .Add(mapper.CompileMappingForAllExplicitlyAddedEntities());


            return true;
        }

        public override void Initialize()
        {
            InitializeInner(Configure.Instance.Configurer);
        }

        void InitializeInner(IConfigureComponents config)
        {
            config.ConfigureComponent<OutboxPersister>(DependencyLifecycle.SingleInstance);
        }
    }

    public class NHibernateSagaPersistence : Feature
    {
        public override bool IsEnabledByDefault
        {
            get { return true; }
        }

        public override bool ShouldBeEnabled()
        {
            return IsEnabled<Sagas>();
        }

    }
}