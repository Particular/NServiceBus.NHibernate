namespace NServiceBus.Persistence.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Reflection;
    using NServiceBus.NHibernate.Internal;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class NHibernateProperties
    {
        private const string connectionString = @"Data Source=nsb;Version=3;New=True;";

        [Test]
        public void Should_assign_default_properties_to_all_persisters()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection();
            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                {
                    new ConnectionStringSettings("NServiceBus/Persistence", connectionString)
                };

            var config = new ConfigureNHibernate(new SettingsHolder());

            var expected = new Dictionary<string, string>
                {
                     {"dialect", ConfigureNHibernate.DefaultDialect},
                     {"connection.connection_string", connectionString}
                   
                };

            CollectionAssert.IsSubsetOf(expected, config.DistributorPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, config.GatewayPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, config.SagaPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, config.SubscriptionStorageProperties);
            CollectionAssert.IsSubsetOf(expected, config.TimeoutPersisterProperties);
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

            var config = new ConfigureNHibernate(new SettingsHolder());

            var expectedForTimeout = new Dictionary<string, string>
                {
                   {"connection.connection_string", "timeout_connection_string"}
                };

            var expectedDefault = new Dictionary<string, string>
                {
                    {"connection.connection_string", connectionString}
                };

            CollectionAssert.IsSubsetOf(expectedDefault, config.DistributorPersisterProperties);
            CollectionAssert.IsSubsetOf(expectedDefault, config.GatewayPersisterProperties);
            CollectionAssert.IsSubsetOf(expectedDefault, config.SagaPersisterProperties);
            CollectionAssert.IsSubsetOf(expectedDefault, config.SubscriptionStorageProperties);
            CollectionAssert.IsSubsetOf(expectedForTimeout, config.TimeoutPersisterProperties);
        }

        [Test]
        public void Should_assign_all_optional_properties_to_all_persisters()
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

            var config = new ConfigureNHibernate(new SettingsHolder());

            var expected = new Dictionary<string, string>
                {
                    {"connection.connection_string", connectionString},
                    {"connection.provider", "provider"},
                    {"connection.driver_class", "driver_class"},
                };

            CollectionAssert.IsSubsetOf(expected, config.DistributorPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, config.GatewayPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, config.SagaPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, config.SubscriptionStorageProperties);
            CollectionAssert.IsSubsetOf(expected, config.TimeoutPersisterProperties);
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
            var config = new ConfigureNHibernate(new SettingsHolder());

            var expected = new Dictionary<string, string>
                {
                    {"connection.connection_string", connectionString},
                };

            CollectionAssert.IsSubsetOf(expected, config.DistributorPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, config.GatewayPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, config.SagaPersisterProperties);
            CollectionAssert.IsSubsetOf(expected, config.SubscriptionStorageProperties);
            CollectionAssert.IsSubsetOf(expected, config.TimeoutPersisterProperties);
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
                    {"connection.connection_string", @"Testing"},
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
                    {"connection.connection_string", @"Testing2"},
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

                var config = new ConfigureNHibernate(new SettingsHolder());
                var configuration =
                    ConfigureNHibernate.CreateConfigurationWith(config.DistributorPersisterProperties);

                return configuration.Properties;
            }
        }

        public class Worker : MarshalByRefObject
        {
            public IDictionary<string, string> Execute()
            {

                var config = new ConfigureNHibernate(new SettingsHolder());
                var configuration =
                    ConfigureNHibernate.CreateConfigurationWith(config.DistributorPersisterProperties);

                return configuration.Properties;
            }
        }
    }
}