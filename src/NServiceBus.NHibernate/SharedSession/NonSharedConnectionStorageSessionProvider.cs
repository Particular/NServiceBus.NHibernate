namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using global::NHibernate;

    class NonSharedConnectionStorageSessionProvider : IStorageSessionProvider
    {
        readonly SessionFactoryProvider sessionFactoryProvider;

        public NonSharedConnectionStorageSessionProvider(SessionFactoryProvider sessionFactoryProvider)
        {
            this.sessionFactoryProvider = sessionFactoryProvider;
        }

        public void ExecuteInTransaction(Action<ISession> operation)
        {
            using (var session = sessionFactoryProvider.SessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                operation(session);
                tx.Commit();
            }
        }
    }
}