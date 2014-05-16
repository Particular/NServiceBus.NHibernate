using NServiceBus;

public class ConfigureOutboxPersister : ConfigurePersistences
{
    public void Configure(Configure config)
    {
        config.UseNHibernateOutbox();
    }
}