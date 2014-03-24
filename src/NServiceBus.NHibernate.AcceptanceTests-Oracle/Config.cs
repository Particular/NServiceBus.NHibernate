using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using NServiceBus;
using NServiceBus.Persistence.NHibernate;
using NServiceBus.Saga;

public abstract class ConfigurePersistences
{
    protected ConfigurePersistences()
    {
        NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
        {
            new ConnectionStringSettings("NServiceBus/Persistence", @"Data Source=XE;User Id=john;Password=Welcome1")
        };

        NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                                                               {
                                                                   {"NServiceBus/Persistence/NHibernate/connection.driver_class", "NHibernate.Driver.OracleDataClientDriver"},
                                                                   {"NServiceBus/Persistence/NHibernate/dialect", "NHibernate.Dialect.Oracle10gDialect"}
                                                               };
    }
}

public class ConfigureTimeoutStorage : ConfigurePersistences
{
    public void Configure(Configure config)
    {
        config.UseNHibernateTimeoutPersister();
    }
}

public class ConfigureSubscriptionStorage : ConfigurePersistences
{
    public void Configure(Configure config)
    {
        config.UseNHibernateSubscriptionPersister();
    }
}

public class ConfigureSagaPersister : ConfigurePersistences
{
    public void Configure(Configure config)
    {
        config.UseNHibernateSagaPersister(type =>
        {
            var tablename = DefaultTableNameConvention(type);

            if (tablename.Length > 30) //Oracle limit and it has to start with an Alpha character
            {
                return Create(tablename);
            }

            return tablename;
        });
    }
    
    static string Create(params object[] data)
    {
        using (var provider = new MD5CryptoServiceProvider())
        {
            var inputBytes = Encoding.Default.GetBytes(String.Concat(data));
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