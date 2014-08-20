using NServiceBus;
using NServiceBus.Persistence;

public class ConfigureNHibernatePersistence
{
    public void Configure(BusConfiguration config)
    {
        config.UsePersistence<NHibernatePersistence>()
            .ConnectionString(@"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;");
    }
}