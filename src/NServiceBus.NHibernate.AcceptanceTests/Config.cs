using System.Configuration;
using NServiceBus;
using NServiceBus.Persistence.NHibernate;

public class ConfigureSagaPersister
{
    public void Configure(Configure config)
    {
        NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
        {
            new ConnectionStringSettings("NServiceBus/Persistence", SqlServerConnectionString)
        };

        config.UseNHibernateSagaPersister();
    }

    static string SqlServerConnectionString = @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;";
}