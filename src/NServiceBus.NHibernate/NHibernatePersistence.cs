namespace NServiceBus
{
    using Features;
    using Persistence;

    /// <summary>
    /// NHibernate persistence for everything.
    /// </summary>
    public partial class NHibernatePersistence : PersistenceDefinition, IPersistenceDefinitionFactory<NHibernatePersistence>
    {
        // constructor parameter is a temporary workaround until the public constructor is removed
        NHibernatePersistence(object _)
        {
            Defaults(static s => s.SetDefault("NHibernate.Common.AutoUpdateSchema", true));

            Supports<StorageType.Sagas, NHibernateSagaStorage>(new StorageType.SagasOptions { SupportsFinders = true });
            Supports<StorageType.Subscriptions, NHibernateSubscriptionStorage>();
            Supports<StorageType.Outbox, NHibernateOutbox>();
        }

        static NHibernatePersistence IPersistenceDefinitionFactory<NHibernatePersistence>.Create() => new(null);
    }
}