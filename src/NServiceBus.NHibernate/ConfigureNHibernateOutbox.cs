namespace NServiceBus
{
    using Outbox.NHibernate;
    using Outbox;
// ReSharper disable RedundantNameQualifier
    using global::NHibernate.Cfg;
// ReSharper restore RedundantNameQualifier
    using Persistence.NHibernate;

    /// <summary>
    /// Configuration extensions for the NHibernate Outbox
    /// </summary>
    public static class ConfigureNHibernateOutbox
    {
        /// <summary>
        /// Configures NHibernate Outbox persister.
        /// </summary>
        /// <remarks>
        /// Reads configuration settings from <a href="http://msdn.microsoft.com/en-us/library/ms228154.aspx">&lt;appSettings&gt; config section</a> and <a href="http://msdn.microsoft.com/en-us/library/bf7sd233">&lt;connectionStrings&gt; config section</a>.
        /// </remarks>
        /// <example>
        /// An example that shows the minimum configuration:
        /// <code lang="XML" escaped="true">
        ///  <appSettings>
        ///    <!-- optional settings examples -->
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
        ///    <add name="NServiceBus/Persistence/NHibernate/Outbox" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True" />
        ///  </connectionStrings>
        /// </code>
        /// </example>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure UseNHibernateOutbox(this Configure config)
        {
            ConfigureNHibernate.ConfigureSqlLiteIfRunningInDebugModeAndNoConfigPropertiesSet(ConfigureNHibernate.OutboxPersisterProperties);

            var properties = ConfigureNHibernate.OutboxPersisterProperties;
            var configuration = ConfigureNHibernate.CreateConfigurationWith(properties);

            return config.UseNHibernateOutboxInternal(configuration);
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        /// <returns>The configuration object</returns>
        public static Configure UseNHibernateOutbox(this Configure config, Configuration configuration)
        {
            foreach (var property in configuration.Properties)
            {
                ConfigureNHibernate.OutboxPersisterProperties[property.Key] = property.Value;
            }

            return config.UseNHibernateOutboxInternal(configuration);
        }

        private static Configure UseNHibernateOutboxInternal(this Configure config, Configuration configuration)
        {
            ConfigureNHibernate.ThrowIfRequiredPropertiesAreMissing(configuration.Properties);

            Installer.RunInstaller = true;

            ConfigureNHibernate.AddMappings<OutboxEntityMap>(configuration);
            ConfigureNHibernate.AddMappings<TransportOperationEntityMap>(configuration);

            Installer.configuration = configuration;

            config.Configurer.ConfigureComponent<OutboxPersister>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.SessionFactory, configuration.BuildSessionFactory());

            return config;
        }
    }
}