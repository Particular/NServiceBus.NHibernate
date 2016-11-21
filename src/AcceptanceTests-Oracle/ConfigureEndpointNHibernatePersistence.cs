using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence;
using NServiceBus.Persistence.NHibernate;

public class ConfigureScenariosForNHibernatePersistence : IConfigureSupportedScenariosForTestExecution
{
    public IEnumerable<Type> UnsupportedScenarioDescriptorTypes { get; } = new List<Type>();
}

public class ConfigureEndpointNHibernatePersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration config, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
        {
            {"NServiceBus/Persistence/NHibernate/connection.driver_class", "NHibernate.Driver.OracleManagedDataClientDriver"},
            {"NServiceBus/Persistence/NHibernate/dialect", "NHibernate.Dialect.Oracle10gDialect"},
            {"NServiceBus/Persistence/NHibernate/show_sql", "true"}
        };

        config.UsePersistence<NHibernatePersistence>()
            .ConnectionString(@"Data Source=(DESCRIPTION=(ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST = 127.0.0.1)(PORT = 1521)))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = XE)));User Id=particular; Password=Welcome1; Enlist=dynamic")
            .SagaTableNamingConvention(type =>
            {
                var tablename = DefaultTableNameConvention(type);

                if (tablename.Length > 30) //Oracle limit and it has to start with an Alpha character
                {
                    return Create(tablename);
                }

                return tablename;
            });

        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        return Task.FromResult(0);
    }

    static string Create(params object[] data)
    {
        using (var provider = new MD5CryptoServiceProvider())
        {
            var inputBytes = Encoding.Default.GetBytes(string.Concat(data));
            var hashBytes = provider.ComputeHash(inputBytes);
            // generate a guid from the hash:
            return "A" + new Guid(hashBytes).ToString("N").Substring(0, 20);
        }
    }

    static string DefaultTableNameConvention(Type type)
    {
        //if the type is nested use the name of the parent
        if (type.DeclaringType == null)
        {
            return type.Name;
        }

        if (typeof(IContainSagaData).IsAssignableFrom(type))
        {
            return type.DeclaringType.Name;
        }

        return type.DeclaringType.Name + "_" + type.Name;
    }
}