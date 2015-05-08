namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using global::NHibernate;

    interface IStorageSessionProvider
    {
        void ExecuteInTransaction(Action<ISession> operation);
    }
}