using NServiceBus;

public class ConfigureSagaPersister : ConfigurePersistences
{
    public void Configure(Configure config)
    {
        config.UseNHibernateSagaPersister();
    }
}