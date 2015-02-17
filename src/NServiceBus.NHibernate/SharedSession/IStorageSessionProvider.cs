namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using global::NHibernate;

    interface IStorageSessionProvider
    {
        IStatelessSession OpenStatelessSession();

        ISession OpenSession();

        void ExecuteInTransaction(Action<ISession> operation);
    }
}