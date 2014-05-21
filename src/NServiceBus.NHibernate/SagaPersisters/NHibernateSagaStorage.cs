namespace NServiceBus.Features
{
    using System;
    using System.Linq;
    using global::NHibernate.Cfg;
    using SagaPersisters.NHibernate;
    using SagaPersisters.NHibernate.AutoPersistence;

    /// <summary>
    /// NHibernate Saga Storage.
    /// </summary>
    public class NHibernateSagaStorage : Feature
    {
        /// <summary>
        /// Creates an instance of <see cref="NHibernateSagaStorage"/>.
        /// </summary>
        public NHibernateSagaStorage()
        {
            DependsOn<Sagas>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
        }

        internal static void ApplyMappings(Configure config, Configuration configuration)
        {
            var tableNamingConvention = config.Settings.GetOrDefault<Func<Type, string>>("NHibernate.Sagas.TableNamingConvention");

            var scannedAssemblies = config.TypesToScan.Select(t => t.Assembly).Distinct();

            foreach (var assembly in scannedAssemblies)
            {
                configuration.AddAssembly(assembly);
            }

            var types = config.TypesToScan.Except(configuration.ClassMappings.Select(x => x.MappedClass));
            SagaModelMapper modelMapper;
            if (tableNamingConvention == null)
            {
                modelMapper = new SagaModelMapper(types);
            }
            else
            {
                modelMapper = new SagaModelMapper(types, tableNamingConvention);
            }

            configuration.AddMapping(modelMapper.Compile());
        }
    }
}