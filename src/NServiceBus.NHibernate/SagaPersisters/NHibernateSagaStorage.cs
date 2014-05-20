namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::NHibernate.Cfg;
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
            

            config.Configurer.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
        }

        class RegisterMappings : INeedInitialization
        {
            public void Init(Configure config)
            {
                var tableNamingConvention = config.Settings.GetOrDefault<Func<Type, string>>("NHibernate.Sagas.TableNamingConvention");

                config.Settings.Get<List<Action<Configuration>>>("StorageConfigurationModifications")
                    .Add(c =>
                    {
                        var scannedAssemblies = config.TypesToScan.Select(t => t.Assembly).Distinct();

                        foreach (var assembly in scannedAssemblies)
                        {
                            c.AddAssembly(assembly);
                        }

                        var types = config.TypesToScan.Except(c.ClassMappings.Select(x => x.MappedClass));
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
            }
        }
    }
}