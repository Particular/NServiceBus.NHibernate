namespace NServiceBus.NHibernate.SharedSession
{
    using global::NHibernate;

    interface ISessionFactoryProvider
    {
        ISessionFactory SessionFactory { get; }
    }
}