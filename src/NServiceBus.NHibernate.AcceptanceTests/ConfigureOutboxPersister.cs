using NServiceBus;
using NServiceBus.Persistence;

public class ConfigureOutboxPersister : ConfigurePersistences
{
    public void Configure(Configure config)
    {
        config.UsePersistence<NServiceBus.Persistence.NHibernate>();
    }
}