using System.Collections.Specialized;
using System.Configuration;
using NServiceBus;
using NServiceBus.Persistence.NHibernate;

public class ConfigureSagaPersister
{
    public void Configure(Configure config)
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

        config.UseNHibernateSagaPersister();
    }
}