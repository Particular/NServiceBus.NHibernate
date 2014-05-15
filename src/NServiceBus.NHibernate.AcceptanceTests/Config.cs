using System.Configuration;
using NServiceBus;
using NServiceBus.NHibernate;
using NServiceBus.NHibernate.Internal;

public abstract class ConfigurePersistences
{
    protected ConfigurePersistences()
    {
        NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
        {
            new ConnectionStringSettings("NServiceBus/Persistence", @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;")
        };
    }
}

public class ConfigureTimeoutStorage : ConfigurePersistences
{
    public void Configure(Configure config)
    {
        config.UseNHibernateTimeoutPersister();
    }
}

public class ConfigureSubscriptionStorage : ConfigurePersistences
{
    public void Configure(Configure config)
    {
        config.UseNHibernateSubscriptionPersister();
    }
}

public class ConfigureSagaPersister
{
    public void Configure(Configure config)
    {
        config.UsePersistence<NServiceBus.Persistence.NHibernate>(c => c.ConnectionString(@"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;"));
    }
}