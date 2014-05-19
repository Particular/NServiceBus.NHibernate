namespace NServiceBus
{
    using System;
    using NHibernate;
    using NHibernate.Internal;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using Persistence;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Unicast.Subscriptions.NHibernate;
    using Unicast.Subscriptions.NHibernate.Config;

    /// <summary>
    /// Configuration extensions for the NHibernate subscription storage
    /// </summary>
    public static class ConfigureNHibernateSubscriptionStorage
    {
        /// <summary>
        /// Configures NHibernate Subscription Persister.
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
        ///    <add name="NServiceBus/Persistence/NHibernate/Subscription" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=subscription;Integrated Security=True" />
        ///  </connectionStrings>
        /// </code>
        /// </example>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5.1", Replacement = "config.UsePersistence<Persistence.NHibernate>();")]
        public static Configure UseNHibernateSubscriptionPersister(this Configure config)
        {
            return config.UsePersistence<Persistence.NHibernate>();
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// DB schema is updated if requested by the user
        /// </summary>
        /// <param name="config">The <see cref="Configure" /> object.</param>
        /// <param name="configuration">The <see cref="Configuration" /> allows the application to specify properties and mapping documents to be used when creating a <see cref="ISessionFactory" />.</param>
        /// <returns>The <see cref="Configure" /> object.</returns>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5.1", Replacement = "config.UsePersistence<Persistence.NHibernate>(c => c.UseSubscriptionStorageConfiguration(configuration));")]
        public static Configure UseNHibernateSubscriptionPersister(this Configure config, Configuration configuration)
        {
            return config.UsePersistence<Persistence.NHibernate>(c => c.UseSubscriptionStorageConfiguration(configuration));
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// DB schema is updated if requested by the user
        /// </summary>
        /// <param name="config">The <see cref="Configure" /> object.</param>
        /// <param name="configuration">The <see cref="Configuration" /> allows the application to specify properties and mapping documents to be used when creating a <see cref="ISessionFactory" />.</param>
        /// <param name="cacheExpiration">The <see cref="TimeSpan" /> to cache subscriptions for.</param>
        /// <returns>The <see cref="Configure" /> object.</returns>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5.1", Replacement = "config.UsePersistence<Persistence.NHibernate>(c =>{c.UseSubscriptionStorageConfiguration(configuration);if (cacheExpiration.HasValue){c.EnableCaching(cacheExpiration.Value);}})")]
        public static Configure UseNHibernateSubscriptionPersister(this Configure config, Configuration configuration, TimeSpan? cacheExpiration)
        {
            return config.UsePersistence<Persistence.NHibernate>(c =>
            {
                c.UseSubscriptionStorageConfiguration(configuration);

                if (cacheExpiration.HasValue)
                {
                    c.EnableCaching(cacheExpiration.Value);
                }
                
            });
        }

        /// <summary>
        /// Disables the automatic creation of the database schema.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5.1", Replacement = "config.UsePersistence<Persistence.NHibernate>(c=>c.DisableSubscriptionStorageSchemaUpdate());")]
        public static Configure DisableNHibernateSubscriptionPersisterInstall(this Configure config)
        {
            return config.UsePersistence<Persistence.NHibernate>(c=>c.DisableSubscriptionStorageSchemaUpdate());
        }
    }
}