using System.Collections.Specialized;
using NServiceBus;
using NServiceBus.NHibernate;
using NServiceBus.NHibernate.Internal;
using NServiceBus.Persistence;

public class ConfigureNHibernatePersistence
{
    public void Configure(Configure config)
    {
        NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
        {
            {"NServiceBus/Persistence/NHibernate/connection.driver_class", "NHibernate.Driver.OracleDataClientDriver"},
            {"NServiceBus/Persistence/NHibernate/dialect", "NHibernate.Dialect.Oracle10gDialect"}
        };
        config.UsePersistence<NServiceBus.Persistence.NHibernate>(c => c.ConnectionString(@"Data Source=XE;User Id=particular;Password=Welcome1"));
    }
}