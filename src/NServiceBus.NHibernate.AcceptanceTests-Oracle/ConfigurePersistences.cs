using System.Collections.Specialized;
using System.Configuration;
using NServiceBus.Persistence.NHibernate;

public abstract class ConfigurePersistences
{
    protected ConfigurePersistences()
    {
        NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
        {
            new ConnectionStringSettings("NServiceBus/Persistence", @"Data Source=XE;User Id=particular;Password=Welcome1")
        };

        NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
        {
            {"NServiceBus/Persistence/NHibernate/connection.driver_class", "NHibernate.Driver.OracleDataClientDriver"},
            {"NServiceBus/Persistence/NHibernate/dialect", "NHibernate.Dialect.Oracle10gDialect"}
        };
    }
}