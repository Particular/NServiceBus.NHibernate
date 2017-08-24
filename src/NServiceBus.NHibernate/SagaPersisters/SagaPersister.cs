namespace NServiceBus.SagaPersisters.NHibernate
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::NHibernate;
    using global::NHibernate.Criterion;
    using Extensibility;
    using Persistence;
    using Sagas;

    class SagaPersister : ISagaPersister
    {
        public Task Save(IContainSagaData saga, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            session.Session().Save(saga);
            return Task.FromResult(0);
        }


        public Task Update(IContainSagaData saga, SynchronizedStorageSession session, ContextBag context)
        {
            session.Session().Update(saga);
            return Task.FromResult(0);
        }

        public Task<T> Get<T>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where T : IContainSagaData
        {
            var result = session.Session().Get<T>(sagaId, GetLockModeForSaga<T>());
            return Task.FromResult(result);
        }

        Task<T> ISagaPersister.Get<T>(string property, object value, SynchronizedStorageSession session, ContextBag context)
        {
            var result = session.Session().CreateCriteria(typeof(T))
                .SetLockMode(GetLockModeForSaga<T>())
                .Add(Restrictions.Eq(property, value))
                .UniqueResult<T>();

            return Task.FromResult(result);
        }
        public Task Complete(IContainSagaData saga, SynchronizedStorageSession session, ContextBag context)
        {
            session.Session().Delete(saga);
            return Task.FromResult(0);
        }

        static LockMode GetLockModeForSaga<T>()
        {
            var explicitLockModeAttribute = typeof(T).GetCustomAttributes(typeof(LockModeAttribute), false).SingleOrDefault();

            if (explicitLockModeAttribute == null)
                return LockMode.Upgrade;//our new default in v4.1.0

            var explicitLockMode = ((LockModeAttribute)explicitLockModeAttribute).RequestedLockMode;

            switch (explicitLockMode)
            {
                case LockModes.Force:
                    return LockMode.Force;
                case LockModes.None:
                    return LockMode.None;
                case LockModes.Read:
                    return LockMode.Read;
                case LockModes.Upgrade:
                    return LockMode.Upgrade;
                case LockModes.UpgradeNoWait:
                    return LockMode.UpgradeNoWait;
                case LockModes.Write:
                    return LockMode.Write;

                default:
                    throw new InvalidOperationException("Unknown lock mode requested: " + explicitLockMode);

            }
        }
    }
}
