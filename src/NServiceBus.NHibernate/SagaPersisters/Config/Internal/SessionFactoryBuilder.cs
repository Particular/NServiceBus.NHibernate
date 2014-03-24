namespace NServiceBus.SagaPersisters.NHibernate.Config.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using AutoPersistence;
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using Configuration = global::NHibernate.Cfg.Configuration;

    /// <summary>
    /// Builder class for the NHibernate Session Factory
    /// </summary>
    public class SessionFactoryBuilder
    {
        private readonly IEnumerable<Type> typesToScan;
        readonly Func<Type, string> tableNamingConvention;

        /// <summary>
        /// Constructor that accepts the types to scan for saga data classes
        /// </summary>
// ReSharper disable IntroduceOptionalParameters.Global
        public SessionFactoryBuilder(IEnumerable<Type> typesToScan) : this(typesToScan, null)
// ReSharper restore IntroduceOptionalParameters.Global
        {
        }

        public SessionFactoryBuilder(IEnumerable<Type> typesToScan, Func<Type, string> tableNamingConvention)
        {
            this.typesToScan = typesToScan;
            this.tableNamingConvention = tableNamingConvention;
        }

        /// <summary>
        /// Builds the session factory with the given properties. Database is updated if updateSchema is set
        /// </summary>
        public ISessionFactory Build(Configuration nhibernateConfiguration)
        {
            var scannedAssemblies = typesToScan.Select(t => t.Assembly).Distinct();

            foreach (var assembly in scannedAssemblies)
            {
                nhibernateConfiguration.AddAssembly(assembly);
            }

            var types = typesToScan.Except(nhibernateConfiguration.ClassMappings.Select(x => x.MappedClass));
            SagaModelMapper modelMapper;
            if (tableNamingConvention == null)
            {
                modelMapper = new SagaModelMapper(types);
            }
            else
            {
                modelMapper = new SagaModelMapper(types, tableNamingConvention);
            }

            nhibernateConfiguration.AddMapping(modelMapper.Compile());

            try
            {
                return nhibernateConfiguration.BuildSessionFactory();
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                    throw new ConfigurationErrorsException(e.InnerException.Message, e);

                throw;
            }
        }
    }
}