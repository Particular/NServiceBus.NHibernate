using System.Configuration;
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