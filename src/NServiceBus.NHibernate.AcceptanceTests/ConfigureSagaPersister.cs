using NServiceBus;
using NServiceBus.Persistence;

public class ConfigureSagaPersister : ConfigurePersistences
{
    public void Configure(Configure config)
    {
        config.UsePersistence<NServiceBus.Persistence.NHibernate>();
    }
}