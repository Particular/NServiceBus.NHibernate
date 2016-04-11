#pragma warning disable 1591
namespace NServiceBus.Persistence.NHibernate
{

    [ObsoleteEx(
        RemoveInVersion = "8", 
        TreatAsErrorFromVersion = "7", 
        ReplacementTypeOrMember = "IMessageHandlingContext.StorageSession()")]
    public class NHibernateStorageContext
    {
    }
}

namespace NServiceBus.Features
{
    [ObsoleteEx(
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7",
        Message = "This feature class was not intented to be used by end users. It's equivalent cannot be disabled in version 7.")]
    public class NHibernateDBConnectionProvider
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7",
        Message = "Feature classes are not exposed any more. To enable only the outbox storage use endpointConfiguration.UsePersistence<NHibernatePersistence, StorageType.Outbox>")]
    public class NHibernateOutboxStorage
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7",
        Message = "Feature classes are not exposed any more. To enable only the subscription storage use endpointConfiguration.UsePersistence<NHibernatePersistence, StorageType.Subscriptions>")]
    public class NHibernateSubscriptionStorage
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7",
        Message = "Feature classes are not exposed any more. To enable only the timeouts storage use endpointConfiguration.UsePersistence<NHibernatePersistence, StorageType.Timeouts>")]
    public class NHibernateTimeoutStorage
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7",
        Message = "This feature class was not intenced to be used by end users. It's equivalent cannot be disabled in version 7.")]
    public class NHibernateStorageSession
    {
    }
}
#pragma warning restore 1591
