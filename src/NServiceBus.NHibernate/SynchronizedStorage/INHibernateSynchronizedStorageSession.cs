namespace NServiceBus
{
    using NHibernate;

    interface INHibernateSynchronizedStorageSession
    {
        ISession Session { get; }
    }
}