namespace NServiceBus.NHibernate.SharedSession
{
    using global::NHibernate;

    interface IStorageSessionProvider
    {
        ISession Session { get; }

        IStatelessSession OpenStatelessSession();
    }
}