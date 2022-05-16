namespace NServiceBus.Persistence.NHibernate
{
    interface INHibernateStorageSessionProvider
    {
        INHibernateStorageSession InternalSession { get; }
    }
}