namespace NServiceBus
{
    using System;

    /// <summary>
    /// Configuration extensions for the NHibernate Gateway deduplication
    /// </summary>
    public static class ConfigureNHibernateGatewayDeduplication
    {
        /// <summary>
        /// Configures NHibernate Gateway deduplication.
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
        ///    <add name="NServiceBus/Persistence/NHibernate/Deduplication" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=gateway;Integrated Security=True" />
        ///  </connectionStrings>
        /// </code>
        /// </example>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5.0", Replacement = "builder.UsePersistence<NHibernatePersistence>().For(Storage.GatewayDeduplication)")]
// ReSharper disable UnusedParameter.Global
        public static Configure UseNHibernateGatewayDeduplication(this Configure config)
// ReSharper restore UnusedParameter.Global
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        /// <returns>The configuration object</returns>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5.0", Replacement = "builder.UsePersistence<NHibernatePersistence>().For(Storage.GatewayDeduplication).UseGatewayDeduplicationConfiguration(configuration)")]
// ReSharper disable UnusedParameter.Global
        public static Configure UseNHibernateGatewayDeduplication(this Configure config, NHibernate.Cfg.Configuration configuration)
// ReSharper restore UnusedParameter.Global
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Disables the automatic creation of the database schema.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5.1", Replacement = "builder.UsePersistence<NHibernatePersistence>().For(Storage.GatewayDeduplication).DisableGatewayDeduplicationSchemaUpdate()")]
// ReSharper disable UnusedParameter.Global
        public static Configure DisableNHibernateGatewayDeduplicationInstall(this Configure config)
// ReSharper restore UnusedParameter.Global
        {
            throw new InvalidOperationException();
        }
    }
}