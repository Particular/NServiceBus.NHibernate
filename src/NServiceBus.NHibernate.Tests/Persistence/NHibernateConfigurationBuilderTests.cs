namespace NServiceBus.Persistence.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Reflection;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class NHibernateConfigurationBuilderTests
    {
        private const string connectionString = @"Data Source=nsb;New=True;";

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
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection();
            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                {
                    new ConnectionStringSettings("NServiceBus/Persistence", connectionString)
                };

            var builder = new NHibernateConfigurationBuilder(new SettingsHolder(), "NotUsed", "NotUsed");

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
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection();
            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                {
                    new ConnectionStringSettings("NServiceBus/Persistence", connectionString),
                    new ConnectionStringSettings("NServiceBus/Persistence/NHibernate/Timeout",
                                                 "timeout_connection_string")
                };

            var builder = new NHibernateConfigurationBuilder(new SettingsHolder(),"Timeout","NotUsed");

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

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                {
                    new ConnectionStringSettings("NServiceBus/Persistence", connectionString)
                };

            var builder = new NHibernateConfigurationBuilder(new SettingsHolder(), "NotUsed", "NotUsed");

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

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                {
                    new ConnectionStringSettings("NServiceBus/Persistence", connectionString)
                };
            var builder = new NHibernateConfigurationBuilder(new SettingsHolder(), "NotUsed","NotUsed");

            var expected = new Dictionary<string, string>
                {
                    {"connection.connection_string", connectionString},
                };

            CollectionAssert.IsSubsetOf(expected, builder.Build().Configuration.Properties);
        }

        [Test]
        public void Should_read_settings_from_hibernate_configuration_config_section_if_available()
        {
            var appDomain = AppDomain.CreateDomain("Testing", AppDomain.CurrentDomain.Evidence,
                                                   new AppDomainSetup
                                                       {
                                                           ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                           ConfigurationFile = "Testing.config"
                                                       });
            
            var worker = (Worker)appDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof (Worker).FullName);
            var result = worker.Execute();
            AppDomain.Unload(appDomain);

            var expected = new Dictionary<string, string>
                {
                    {"connection.connection_string", @"Data Source=:memory:;New=True;"},
                };
            
            CollectionAssert.IsSubsetOf(expected, result);
        }

        [Test]
        public void Should_read_settings_from_hibernate_cfg_xml_if_available()
        {
            var appDomain = AppDomain.CreateDomain("Testing", AppDomain.CurrentDomain.Evidence,
                                                   new AppDomainSetup
                                                   {
                                                       ApplicationBase =
                                                           AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                   });

            var worker = (Worker)appDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(Worker).FullName);
            var result = worker.Execute();
            AppDomain.Unload(appDomain);

            var expected = new Dictionary<string, string>
                {
                    {"connection.connection_string", @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;"},
                };

            CollectionAssert.IsSubsetOf(expected, result);
        }

        [Test]
        public void Our_settings_should_take_precedence_over_settings_from_hibernate_cfg_xml_if_available()
        {
            var appDomain = AppDomain.CreateDomain("Testing", AppDomain.CurrentDomain.Evidence,
                                                   new AppDomainSetup
                                                   {
                                                       ApplicationBase =
                                                           AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                   });

            var worker = (Worker2)appDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(Worker2).FullName);
            var result = worker.Execute();
            AppDomain.Unload(appDomain);

            var expected = new Dictionary<string, string>
                {
                    {"connection.connection_string", "specified"},
                };

            CollectionAssert.IsSubsetOf(expected, result);
        }

        [Test]
        public void Our_settings_should_take_precedence_over_settings_from_hibernate_configuration_config_section_if_available()
        {
            var appDomain = AppDomain.CreateDomain("Testing", AppDomain.CurrentDomain.Evidence,
                                                   new AppDomainSetup
                                                   {
                                                       ConfigurationFile = "Testing.config",
                                                       ApplicationBase =
                                                           AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                   });

            var worker = (Worker2)appDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(Worker2).FullName);
            var result = worker.Execute();
            AppDomain.Unload(appDomain);

            var expected = new Dictionary<string, string>
                {
                    {"connection.connection_string", "specified"},
                };

            CollectionAssert.IsSubsetOf(expected, result);
        }

        public class Worker2 : MarshalByRefObject
        {
            public IDictionary<string, string> Execute()
            {
                NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                {
                    new ConnectionStringSettings("NServiceBus/Persistence", "specified")
                };

                var builder = new NHibernateConfigurationBuilder(new SettingsHolder(), "NotUsed", "NotUsed");
                return builder.Build().Configuration.Properties;
            }
        }

        public class Worker : MarshalByRefObject
        {
            public IDictionary<string, string> Execute()
            {
                var builder = new NHibernateConfigurationBuilder(new SettingsHolder(), "NotUsed", "NotUsed");
                return builder.Build().Configuration.Properties;
            }
        }
    }
}