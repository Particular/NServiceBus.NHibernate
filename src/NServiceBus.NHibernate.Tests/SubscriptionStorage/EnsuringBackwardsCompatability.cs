namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Linq;
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;
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
        }

        [Test]
        public void UseNHibernateSubscriptionPersister_Reads_From_AppSettings_And_ConnectionStrings()
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
                                                                             "NServiceBus/Persistence/NHibernate",
                                                                             connectionString)
                                                                     };

            ConfigureNHibernate.Init();

            Configure.With(Enumerable.Empty<Type>())
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .UseNHibernateSubscriptionPersister();

            var expected = new Dictionary<string, string>
                               {
                                   {"dialect", dialect},
                                   {"connection.connection_string", connectionString},
                               };

            CollectionAssert.IsSubsetOf(expected, ConfigureNHibernate.SubscriptionStorageProperties);
        }

    }
}