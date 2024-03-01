namespace NServiceBus.Persistence.NHibernate.Tests
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Dynamic;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class NHibernateConfigurationBuilderTests
    {
        const string connectionString = "Data Source=nsb;New=True;";

        [Test]
        public void Should_fail_validation_if_no_connection_string_is_defined()
        {
            Assert.IsFalse(NHibernateConfigurationBuilder.ContainsRequiredProperties(new Dictionary<string, string>()));
        }

        [Test]
        public void Should_pass_validation_if_connection_string_is_defined_literally()
        {
            Assert.IsTrue(NHibernateConfigurationBuilder.ContainsRequiredProperties(new Dictionary<string, string>
                                                                                                  {
                                                                                                      {"connection.connection_string", "aString"}
                                                                                                  }));
        }

        [Test]
        public void Should_pass_validation_if_connection_string_is_defined_by_name()
        {
            Assert.IsTrue(NHibernateConfigurationBuilder.ContainsRequiredProperties(new Dictionary<string, string>
                                                                                                  {
                                                                                                      {"connection.connection_string_name", "aString"}
                                                                                                  }));
        }

        [Test]
        public void Should_assign_default_properties()
        {
            NHibernateSettingRetriever.AppSettings = () => [];

#pragma warning disable IDE0028 // Simplify collection initialization
            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
            {
                    new ConnectionStringSettings("NServiceBus/Persistence", connectionString)
            };
#pragma warning restore IDE0028 // Simplify collection initialization

            var builder = new NHibernateConfigurationBuilder(new SettingsHolder(), new ExpandoObject(), "NotUsed", "NotUsed");

            var expected = new Dictionary<string, string>
                {
                     {"dialect", NHibernateConfigurationBuilder.DefaultDialect},
                     {"connection.connection_string", connectionString}
                };

            CollectionAssert.IsSubsetOf(expected, builder.Build().Configuration.Properties);
        }

        [Test]
        public void Should_assign_overridden_connectionString_if_specified()
        {
            NHibernateSettingRetriever.AppSettings = () => [];

#pragma warning disable IDE0028 // Simplify collection initialization
            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                {
                    new ConnectionStringSettings("NServiceBus/Persistence", connectionString),
                    new ConnectionStringSettings("NServiceBus/Persistence/NHibernate/Timeout",
                                                 "timeout_connection_string")
                };
#pragma warning restore IDE0028 // Simplify collection initialization

            var builder = new NHibernateConfigurationBuilder(new SettingsHolder(), new ExpandoObject(), "Timeout", "NotUsed");

            var expected = new Dictionary<string, string>
                {
                   {"connection.connection_string", "timeout_connection_string"}
                };

            CollectionAssert.IsSubsetOf(expected, builder.Build().Configuration.Properties);
        }

        [Test]
        public void Should_assign_all_optional_properties()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                {
                    {"NServiceBus/Persistence/NHibernate/connection.provider", "provider"},
                    {"NServiceBus/Persistence/NHibernate/connection.driver_class", "driver_class"},
                };

#pragma warning disable IDE0028 // Simplify collection initialization
            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                {
                    new ConnectionStringSettings("NServiceBus/Persistence", connectionString)
                };
#pragma warning restore IDE0028 // Simplify collection initialization

            var builder = new NHibernateConfigurationBuilder(new SettingsHolder(), new ExpandoObject(), "NotUsed", "NotUsed");

            var expected = new Dictionary<string, string>
                {
                    {"connection.connection_string", connectionString},
                    {"connection.provider", "provider"},
                    {"connection.driver_class", "driver_class"},
                };

            CollectionAssert.IsSubsetOf(expected, builder.Build().Configuration.Properties);
        }

        [Test]
        public void Should_skip_settings_that_are_not_for_persistence()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                {
                    {"myOtherSetting1", "provider"},
                    {"myOtherSetting2", "driver_class"},
                };

#pragma warning disable IDE0028 // Simplify collection initialization
            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                {
                    new ConnectionStringSettings("NServiceBus/Persistence", connectionString)
                };
#pragma warning restore IDE0028 // Simplify collection initialization

            var builder = new NHibernateConfigurationBuilder(new SettingsHolder(), new ExpandoObject(), "NotUsed", "NotUsed");

            var expected = new Dictionary<string, string>
                {
                    {"connection.connection_string", connectionString},
                };

            CollectionAssert.IsSubsetOf(expected, builder.Build().Configuration.Properties);
        }
    }
}