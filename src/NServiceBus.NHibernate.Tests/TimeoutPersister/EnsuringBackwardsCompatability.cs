namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Linq;
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.NHibernate.Internal;
    using NUnit.Framework;
    using Persistence.NHibernate;

    [TestFixture]
    public class EnsuringBackwardsCompatibility
    {
        private const string connectionString = @"Data Source=.\database.sqlite;Version=3;New=True;";
        private const string dialect = "NHibernate.Dialect.SQLiteDialect";

        [SetUp]
        public void Setup()
        {
            NHibernateSettingRetriever.AppSettings = () => null;
            NHibernateSettingRetriever.ConnectionStrings = () => null;

            Configure.ConfigurationSource = new DefaultConfigurationSource();
        }

        [Test]
        public void UseNHibernateTimeoutPersister_Reads_From_AppSettings_And_ConnectionStrings()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                                                               {
                                                                   {
                                                                       "NServiceBus/Persistence/NHibernate/dialect",
                                                                       dialect
                                                                       }
                                                               };

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                                                                     {
                                                                         new ConnectionStringSettings(
                                                                             "NServiceBus/Persistence",
                                                                             connectionString)
                                                                     };

            ConfigureNHibernate.Init();

            Configure.With(Enumerable.Empty<Type>())
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .UseNHibernateTimeoutPersister();

            var expected = new Dictionary<string, string>
                               {
                                   {"dialect", dialect},
                                   {"connection.connection_string", connectionString},
                               };

            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.TimeoutPersisterProperties);
        }

#pragma warning disable 0618
        [Test]
        public void UseNHibernateTimeoutPersisterWithSQLiteAndAutomaticSchemaGeneration_Automatically_Configure_SqlLite()
        {
            ConfigureNHibernate.Init();

            Configure.With(Enumerable.Empty<Type>())
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .UseNHibernateTimeoutPersisterWithSQLiteAndAutomaticSchemaGeneration();

            var expected = new Dictionary<string, string>
                               {
                                   {"dialect", dialect},
                                   {"connection.connection_string", "Data Source=.\\NServiceBus.Timeouts.sqlite;Version=3;New=True;"},
                               };

            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.TimeoutPersisterProperties);
        }
#pragma warning restore 0618
    }
}