using NServiceBus;
using NServiceBus.Persistence;

public class ConfigureTimeoutStorage : ConfigurePersistences
{
    public void Configure(Configure config)
    {
        config.UsePersistence<NServiceBus.Persistence.NHibernate>();
    }
}