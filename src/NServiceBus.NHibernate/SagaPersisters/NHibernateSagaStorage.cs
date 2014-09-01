namespace NServiceBus.Features
{
    using System;
    using System.IO;
    using System.Linq;
    using NHibernate.Cfg;
    using NHibernate.Mapping.ByCode;
    using SagaPersisters.NHibernate;
    using SagaPersisters.NHibernate.AutoPersistence;
    using Settings;

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
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Settings.Get<SharedMappings>()
                .AddMapping(c => ApplyMappings(context.Settings,c));

            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
        }

        internal void ApplyMappings(ReadOnlySettings settings, Configuration configuration)
        {
            var tableNamingConvention = settings.GetOrDefault<Func<Type, string>>("NHibernate.Sagas.TableNamingConvention");

            var scannedAssemblies = settings.GetAvailableTypes().Select(t => t.Assembly).Distinct();

            foreach (var assembly in scannedAssemblies)
            {
                configuration.AddAssembly(assembly);
            }

            var types = settings.GetAvailableTypes().Except(configuration.ClassMappings.Select(x => x.MappedClass));
            SagaModelMapper modelMapper;
            if (tableNamingConvention == null)
            {
                modelMapper = new SagaModelMapper(types);
            }
            else
            {
                modelMapper = new SagaModelMapper(types, tableNamingConvention);
            }

            var mappingDocument = modelMapper.Compile();

            File.WriteAllText(@"c:\temp\foo.txt", mappingDocument.AsString());
            configuration.AddMapping(mappingDocument);
        }
    }
}