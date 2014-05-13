namespace NServiceBus.UnitOfWork.NHibernate
{
    using global::NHibernate;

    interface IStorageSessionProvider
    {
        ISession Session { get; }
    }
}