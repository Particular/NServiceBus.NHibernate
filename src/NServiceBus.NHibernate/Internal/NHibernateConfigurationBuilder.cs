namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using global::NHibernate.Cfg;
    using global::NHibernate.Cfg.ConfigurationSchema;
    using global::NHibernate.Mapping.ByCode;
    using Settings;
    using Configuration = global::NHibernate.Cfg.Configuration;
    using Environment = global::NHibernate.Cfg.Environment;


    class NHibernateConfigurationBuilder
    {
        static Regex PropertyRetrievalRegex = new Regex(@"NServiceBus/Persistence/NHibernate/([\W\w]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static ConnectionStringSettingsCollection connectionStringSettingsCollection;
        public const string DefaultDialect = "NHibernate.Dialect.MsSql2008Dialect";

        readonly Configuration configuration;

        public NHibernateConfigurationBuilder(ReadOnlySettings settings, dynamic diagnosticsObject, string connectionStringKeySuffix, string specificConfigSetting)
        {
            if (settings.TryGet(specificConfigSetting, out configuration))
            {
                ValidateConfigurationViaCode(configuration.Properties);

                diagnosticsObject.ConfigurationFromCode = true;
                diagnosticsObject.SharedConfig = false;
            }
            else if (settings.TryGet("StorageConfiguration", out configuration))
            {
                ValidateConfigurationViaCode(configuration.Properties);

                diagnosticsObject.ConfigurationFromCode = true;
                diagnosticsObject.SharedConfig = false;
            }
            else
            {
                var configurationProperties = InitFromConfiguration(settings);
                var overriddenProperties = OverrideConnectionStringSettingIfNotNull(configurationProperties, connectionStringKeySuffix);
                configuration = new Configuration().SetProperties(overriddenProperties);
                ValidateConfigurationViaConfigFile(configuration, connectionStringKeySuffix);

                diagnosticsObject.ConfigurationFromAppSettings = true;
            }

            AddDialect(diagnosticsObject, configuration);
        }

        static void AddDialect(dynamic diagnosticsObject, Configuration configuration)
        {
            if (configuration.Properties.TryGetValue(Environment.Dialect, out var dialect))
            {
                diagnosticsObject.Dialect = dialect;
            }
        }

        public NHibernateConfiguration Build()
        {
            if (!configuration.Properties.TryGetValue(Environment.ConnectionString, out string connString))
            {

                if (configuration.Properties.TryGetValue(Environment.ConnectionStringName, out string connStringName))
                {
                    var connectionStringSettings = ConfigurationManager.ConnectionStrings[connStringName];

                    connString = connectionStringSettings.ConnectionString;
                }
            }
            return new NHibernateConfiguration(configuration, connString);
        }

        static IDictionary<string, string> InitFromConfiguration(ReadOnlySettings settings)
        {
            connectionStringSettingsCollection = NHibernateSettingRetriever.ConnectionStrings() ??
                                                 new ConnectionStringSettingsCollection();

            var configuration = CreateNHibernateConfiguration();

            var defaultConnectionString = settings.GetOrDefault<string>("NHibernate.Common.ConnectionString") ?? GetConnectionStringOrNull("NServiceBus/Persistence");
            var configurationProperties = configuration.Properties;

            var appSettingsSection = NHibernateSettingRetriever.AppSettings() ?? new NameValueCollection();
            foreach (string appSetting in appSettingsSection)
            {
                var match = PropertyRetrievalRegex.Match(appSetting);
                if (match.Success)
                {
                    configurationProperties[match.Groups[1].Value] = appSettingsSection[appSetting];
                }
            }
            if (!string.IsNullOrEmpty(defaultConnectionString))
            {
                configurationProperties[Environment.ConnectionString] = defaultConnectionString;
            }

            if (!configurationProperties.ContainsKey(Environment.Dialect))
            {
                configurationProperties[Environment.Dialect] = DefaultDialect;
            }
            return configurationProperties;
        }

        public void AddMappings<T>() where T : IConformistHoldersProvider, new()
        {
            var mapper = new ModelMapper();
            mapper.AddMapping<T>();
            var mappings = mapper.CompileMappingForAllExplicitlyAddedEntities();

            configuration.AddMapping(mappings);
        }

        static void ValidateConfigurationViaCode(IDictionary<string, string> props)
        {
            if (ContainsRequiredProperties(props))
            {
                return;
            }

            const string errorMsg = "When providing a custom Configuration object you need to at least specify the connection string either via " +
                                    Environment.ConnectionString + " or " + Environment.ConnectionStringName + ".";

            throw new InvalidOperationException(errorMsg);
        }

        public static bool ContainsRequiredProperties(IDictionary<string, string> props)
        {
            return props.ContainsKey(Environment.ConnectionString) || props.ContainsKey(Environment.ConnectionStringName);
        }

        static void ValidateConfigurationViaConfigFile(Configuration configuration, string configPrefix)
        {
            if (ContainsRequiredProperties(configuration.Properties))
            {
                return;
            }

            const string errorMsg = @"In order to use NServiceBus with NHibernate you need to provide at least one connection string. You can do it via (in order of precedence):
 * specifying 'NServiceBus/Persistence/NHibernate/{0}' connection string for the {0} persister
 * specifying 'NServiceBus/Persistence' connection string that applies to all persisters
 * specifying 'NServiceBus/Persistence/connection.connection_string' or 'NServiceBus/Persistence/connection.connection_string_name' value in AppSettings or your NHibernate configuration file.
For most scenarios the 'NServiceBus/Persistence' connection string is the best option.";

            throw new InvalidOperationException(string.Format(errorMsg, configPrefix));
        }

        static Configuration CreateNHibernateConfiguration()
        {
            var configuration = new Configuration();
            if (ConfigurationManager.GetSection(CfgXmlHelper.CfgSectionName) is IHibernateConfiguration hc && hc.SessionFactory != null)
            {
                configuration = configuration.Configure();
            }
            else if (File.Exists(GetDefaultConfigurationFilePath()))
            {
                configuration = configuration.Configure();
            }
            return configuration;
        }

        static string GetDefaultConfigurationFilePath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Note RelativeSearchPath can be null even if the doc say something else; don't remove the check
            // ReSharper disable once ConstantNullCoalescingCondition
            var searchPath = AppDomain.CurrentDomain.RelativeSearchPath ?? string.Empty;

            var relativeSearchPath = searchPath.Split(';').First();
            var binPath = Path.Combine(baseDir, relativeSearchPath);
            return Path.Combine(binPath, Configuration.DefaultHibernateCfgFileName);
        }

        static IDictionary<string, string> OverrideConnectionStringSettingIfNotNull(IDictionary<string, string> configurationProperties, string connectionStringSuffix)
        {
            var connectionStringOverride = GetConnectionStringOrNull("NServiceBus/Persistence/NHibernate/" + connectionStringSuffix);

            if (string.IsNullOrEmpty(connectionStringOverride))
            {
                return new Dictionary<string, string>(configurationProperties);
            }

            var overriddenProperties = new Dictionary<string, string>(configurationProperties)
            {
                [Environment.ConnectionString] = connectionStringOverride
            };

            return overriddenProperties;
        }

        static string GetConnectionStringOrNull(string name)
        {
            var connectionStringSettings = connectionStringSettingsCollection[name];

            return connectionStringSettings?.ConnectionString;
        }
    }
}
