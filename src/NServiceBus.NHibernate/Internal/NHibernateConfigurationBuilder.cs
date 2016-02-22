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
        static readonly Regex PropertyRetrievalRegex = new Regex(@"NServiceBus/Persistence/NHibernate/([\W\w]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static ConnectionStringSettingsCollection connectionStringSettingsCollection;
        public const string DefaultDialect = "NHibernate.Dialect.MsSql2008Dialect";

        readonly Configuration configuration;

        public NHibernateConfigurationBuilder(ReadOnlySettings settings, string connectionStringKeySuffix, params string[] settingsKeys)
        {
            configuration = settingsKeys.Select(settings.GetOrDefault<Configuration>).FirstOrDefault(x => x != null);
            if (configuration == null)
            {
                var configurationProperties = InitFromConfiguration(settings);
                var overriddenProperties = OverrideConnectionStringSettingIfNotNull(configurationProperties, connectionStringKeySuffix);
                configuration = new Configuration().SetProperties(overriddenProperties);
                ValidateConfigurationViaConfigFile(configuration, connectionStringKeySuffix);
            }
            else
            {
                ValidateConfigurationViaCode(configuration.Properties);
            }
        }

        public NHibernateConfiguration Build()
        {
            string connString;
            if (!configuration.Properties.TryGetValue(Environment.ConnectionString, out connString))
            {
                string connStringName;

                if (configuration.Properties.TryGetValue(Environment.ConnectionStringName, out connStringName))
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
            if (!String.IsNullOrEmpty(defaultConnectionString))
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

            const string errorMsg = @"When providing a custom Configuration object you need to at least specify the connection string either via " +
                                    Environment.ConnectionString + " or " + Environment.ConnectionStringName + ".";

            throw new InvalidOperationException(errorMsg);
        }

        public static bool ContainsRequiredProperties(IDictionary<string, string> props)
        {
            return (props.ContainsKey(Environment.ConnectionString) || props.ContainsKey(Environment.ConnectionStringName));
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
            var hc = ConfigurationManager.GetSection(CfgXmlHelper.CfgSectionName) as IHibernateConfiguration;
            if (hc != null && hc.SessionFactory != null)
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
            var connectionStringOverride = GetConnectionStringOrNull("NServiceBus/Persistence/NHibernate/"+connectionStringSuffix);

            if (String.IsNullOrEmpty(connectionStringOverride))
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

            return connectionStringSettings == null 
                ? null 
                : connectionStringSettings.ConnectionString;
        }
    }
}
