namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::NHibernate.Cfg;
    using NHibernate.Internal;
    using ObjectBuilder;
    using SagaPersisters.NHibernate;
    using SagaPersisters.NHibernate.AutoPersistence;
    using Settings;

    public class NHibernateSagaStorage : Feature
    {
        public override bool ShouldBeEnabled()
        {
            return IsEnabled<Sagas>();
        }

        public override void Initialize()
        {
            InitializeInner(Configure.Instance.Configurer);
        }

        void InitializeInner(IConfigureComponents config)
        {
            var tableNamingConvention = SettingsHolder.GetOrDefault<Func<Type, string>>("NHibernate.Sagas.TableNamingConvention");

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

            foreach (var kvp in ConfigureNHibernate.SagaPersisterProperties)
            {
                SettingsHolder.Get<Dictionary<string, string>>("StorageProperties")[kvp.Key] = kvp.Value;
            }

            config.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
        }
    }
}