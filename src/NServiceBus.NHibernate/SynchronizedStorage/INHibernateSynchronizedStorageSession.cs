namespace NServiceBus
{
    using global::NHibernate;

    interface INHibernateSynchronizedStorageSession
    {
        ISession Session { get; }
    }
}