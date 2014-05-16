using NServiceBus;

public class ConfigureSubscriptionStorage : ConfigurePersistences
{
    public void Configure(Configure config)
    {
        config.UseNHibernateSubscriptionPersister();
    }
}