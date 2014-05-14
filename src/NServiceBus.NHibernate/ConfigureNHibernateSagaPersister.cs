namespace NServiceBus
{
    using System;
    using Config;
    // ReSharper disable RedundantNameQualifier
    using global::NHibernate;
    using Configuration = global::NHibernate.Cfg.Configuration;
    // ReSharper restore RedundantNameQualifier
    using Persistence.NHibernate;
    using SagaPersisters.NHibernate;
    using SagaPersisters.NHibernate.Config.Internal;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure for the NHibernate saga persister.
    /// </summary>
    public static class ConfigureNHibernateSagaPersister
    {
        /// <summary>
        /// Configures NHibernate Saga Persister.
        /// </summary>
        /// <remarks>
        /// Reads configuration settings from <a href="http://msdn.microsoft.com/en-us/library/ms228154.aspx">&lt;appSettings&gt; config section</a> and <a href="http://msdn.microsoft.com/en-us/library/bf7sd233">&lt;connectionStrings&gt; config section</a>.
        /// </remarks>
        /// <example>
        /// An example that shows the minimum configuration:
        /// <code lang="XML" escaped="true">
        ///  <appSettings>
        ///    <!-- other optional settings examples -->
        ///    <add key="NServiceBus/Persistence/NHibernate/connection.provider" value="NHibernate.Connection.DriverConnectionProvider"/>
        ///    <add key="NServiceBus/Persistence/NHibernate/connection.driver_class" value="NHibernate.Driver.Sql2008ClientDriver"/>
        ///    <!-- For more setting see http://www.nhforge.org/doc/nh/en/#configuration-hibernatejdbc and http://www.nhforge.org/doc/nh/en/#configuration-optional -->
        ///  </appSettings>
        ///  
        ///  <connectionStrings>
        ///    <!-- Default connection string for all persisters -->
        ///    <add name="NServiceBus/Persistence/NHibernate" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True" />
        ///    
        ///    <!-- Optional overrides per persister -->
        ///    <add name="NServiceBus/Persistence/NHibernate/Saga" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=sagas;Integrated Security=True" />
        ///  </connectionStrings>
        /// </code>
        /// </example>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure UseNHibernateSagaPersister(this Configure config)
        {
            return config;//.UseNHibernateSagaPersister((Func<Type, string>)null);
        }

        /// <summary>
        /// Configures NHibernate Saga Persister.
        /// </summary>
        /// <remarks>
        /// Reads configuration settings from <a href="http://msdn.microsoft.com/en-us/library/ms228154.aspx">&lt;appSettings&gt; config section</a> and <a href="http://msdn.microsoft.com/en-us/library/bf7sd233">&lt;connectionStrings&gt; config section</a>.
        /// </remarks>
        /// <example>
        /// An example that shows the minimum configuration:
        /// <code lang="XML" escaped="true">
        ///  <appSettings>
        ///    <!-- other optional settings examples -->
        ///    <add key="NServiceBus/Persistence/NHibernate/connection.provider" value="NHibernate.Connection.DriverConnectionProvider"/>
        ///    <add key="NServiceBus/Persistence/NHibernate/connection.driver_class" value="NHibernate.Driver.Sql2008ClientDriver"/>
        ///    <!-- For more setting see http://www.nhforge.org/doc/nh/en/#configuration-hibernatejdbc and http://www.nhforge.org/doc/nh/en/#configuration-optional -->
        ///  </appSettings>
        ///  
        ///  <connectionStrings>
        ///    <!-- Default connection string for all persisters -->
        ///    <add name="NServiceBus/Persistence/NHibernate" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True" />
        ///    
        ///    <!-- Optional overrides per persister -->
        ///    <add name="NServiceBus/Persistence/NHibernate/Saga" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=sagas;Integrated Security=True" />
        ///  </connectionStrings>
        /// </code>
        /// </example>
        /// <param name="config">The configuration object.</param>
        /// <param name="tableNamingConvention">Convention to use for naming tables.</param>
        /// <returns>The configuration object.</returns>
        public static Configure UseNHibernateSagaPersister(this Configure config, Func<Type, string> tableNamingConvention)
        {
            //var configSection = Configure.GetConfigSection<NHibernateSagaPersisterConfig>();

            //if (configSection != null)
            //{
            //    if (configSection.NHibernateProperties.Count == 0)
            //    {
            //        throw new InvalidOperationException(
            //            "No NHibernate properties found. Please specify NHibernateProperties in your NHibernateSagaPersisterConfig section");
            //    }

            //    foreach (var property in configSection.NHibernateProperties.ToProperties())
            //    {
            //        ConfigureNHibernate.SagaPersisterProperties[property.Key] = property.Value;
            //    }
            //}

            //ConfigureNHibernate.ConfigureSqlLiteIfRunningInDebugModeAndNoConfigPropertiesSet(ConfigureNHibernate.SagaPersisterProperties);

            //var properties = ConfigureNHibernate.SagaPersisterProperties;

            return config;//.UseNHibernateSagaPersisterInternal(ConfigureNHibernate.CreateConfigurationWith(properties), configSection == null || configSection.UpdateSchema, tableNamingConvention);
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// DB schema is updated if requested by the user
        /// </summary>
        /// <param name="config">The <see cref="Configure" /> object.</param>
        /// <param name="configuration">The <see cref="Configuration" /> allows the application to specify properties and mapping documents to be used when creating a <see cref="ISessionFactory" />.</param>
        /// <returns>The <see cref="Configure" /> object.</returns>
        public static Configure UseNHibernateSagaPersister(this Configure config, Configuration configuration)
        {
            return config;//.UseNHibernateSagaPersister(configuration, null);
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// DB schema is updated if requested by the user
        /// </summary>
        /// <param name="config">The <see cref="Configure" /> object.</param>
        /// <param name="configuration">The <see cref="Configuration" /> allows the application to specify properties and mapping documents to be used when creating a <see cref="ISessionFactory" />.</param>
        /// <param name="tableNamingConvention">Convention to use for naming tables.</param>
        /// <returns>The <see cref="Configure" /> object.</returns>
        public static Configure UseNHibernateSagaPersister(this Configure config, Configuration configuration, Func<Type, string> tableNamingConvention)
        {
            foreach (var property in configuration.Properties)
            {
                ConfigureNHibernate.SagaPersisterProperties[property.Key] = property.Value;
            }

            return config;//.UseNHibernateSagaPersisterInternal(configuration, true, tableNamingConvention);
        }

        static Configure UseNHibernateSagaPersisterInternal(this Configure config, Configuration configuration, bool autoUpdateSchema, Func<Type, string> tableNamingConvention = null)
        {
            //ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(ConfigureNHibernate.SagaPersisterProperties);

            //SagaPersisters.NHibernate.Config.Installer.Installer.RunInstaller = autoUpdateSchema;

            //var builder = new SessionFactoryBuilder(Configure.TypesToScan, tableNamingConvention);
            //var sessionFactory = builder.Build(configuration);

            //SagaPersisters.NHibernate.Config.Installer.Installer.configuration = configuration;

            //if (sessionFactory == null)
            //{
            //    throw new InvalidOperationException("Could not create session factory for saga persistence.");
            //}

            //config.Configurer.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);


            return config;
        }

    }
}
