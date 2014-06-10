using NServiceBus;
using NServiceBus.Persistence;

public class ConfigureNHibernatePersistence
{
    public void Configure(Configure config)
    {
        config.UsePersistence<NServiceBus.NHibernate>(c => c.ConnectionString(@"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;"));
    }
}