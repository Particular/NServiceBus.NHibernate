namespace NServiceBus.Persistence.NHibernate
{
    using global::NHibernate;

    interface IStorageSessionProvider
    {
        ISession Session { get; }

        IStatelessSession OpenStatelessSession();

        ISession OpenSession();
    }
}