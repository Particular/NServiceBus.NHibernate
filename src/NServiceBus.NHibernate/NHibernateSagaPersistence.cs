namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::NHibernate.Cfg;
    using ObjectBuilder;
    using SagaPersisters.NHibernate;
    using SagaPersisters.NHibernate.AutoPersistence;
    using Settings;

    public class NHibernateSagaPersistence : Feature
    {
        public override bool IsEnabledByDefault
        {
            get { return true; }
        }

        public override bool ShouldBeEnabled()
        {
            if(!IsEnabled<Sagas>())
            {
                return false;
            }

            Func<Type,string> tableNamingConvention = null;

            SettingsHolder.Get<List<Action<Configuration>>>("StorageConfigurationModifications")
                .Add(c =>
                {
                    var scannedAssemblies = Configure.TypesToScan.Select(t => t.Assembly).Distinct();

                    foreach (var assembly in scannedAssemblies)
                    {
                        c.AddAssembly(assembly);
                    }

                    var types = Configure.TypesToScan.Except(c.ClassMappings.Select(x => x.MappedClass));
                    SagaModelMapper modelMapper;
                    if (tableNamingConvention == null)
                    {
                        modelMapper = new SagaModelMapper(types);
                    }
                    else
                    {
                        modelMapper = new SagaModelMapper(types, tableNamingConvention);
                    }

                    c.AddMapping(modelMapper.Compile());

                    
                });


            return true;
        }

        public override void Initialize()
        {
            InitializeInner(Configure.Instance.Configurer);
        }

        void InitializeInner(IConfigureComponents config)
        {
            config.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
        }
    }
}