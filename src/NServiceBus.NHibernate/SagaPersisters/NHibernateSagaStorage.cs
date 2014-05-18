namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::NHibernate.Cfg;
    using NHibernate.Internal;
    using SagaPersisters.NHibernate;
    using SagaPersisters.NHibernate.AutoPersistence;

    public class NHibernateSagaStorage : Feature
    {
        public NHibernateSagaStorage()
        {
            DependsOn<Sagas>();
        }

        public override void Initialize(Configure config)
        {
            var tableNamingConvention = config.Settings.GetOrDefault<Func<Type, string>>("NHibernate.Sagas.TableNamingConvention");

            config.Settings.Get<List<Action<Configuration>>>("StorageConfigurationModifications")
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
                config.Settings.Get<Dictionary<string, string>>("StorageProperties")[kvp.Key] = kvp.Value;
            }

            config.Configurer.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
        }
    }
}