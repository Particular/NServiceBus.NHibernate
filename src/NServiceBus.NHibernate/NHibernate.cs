namespace NServiceBus
{
    using Persistence;

    /// <summary>
    /// NHibernate persistence for everything.
    /// </summary>
    public class NHibernate : PersistenceDefinition
    {
        /// <summary>
        /// Constructor that defines the capabilities of the storage
        /// </summary>
        public NHibernate()
        {
            HasGatewaysStorage = true;
            HasOutboxStorage = true;
            HasSagaStorage = true;
            HasSubscriptionStorage = true;
            HasTimeoutStorage = true;
        }
    }
}