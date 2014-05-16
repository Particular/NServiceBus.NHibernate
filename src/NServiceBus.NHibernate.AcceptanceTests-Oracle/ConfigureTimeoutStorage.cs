using NServiceBus;

public class ConfigureTimeoutStorage : ConfigurePersistences
{
    public void Configure(Configure config)
    {
        config.UseNHibernateTimeoutPersister();
    }
}