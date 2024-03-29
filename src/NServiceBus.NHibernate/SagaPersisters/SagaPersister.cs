namespace NServiceBus.SagaPersisters.NHibernate
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using global::NHibernate;
    using global::NHibernate.Criterion;
    using Persistence;
    using Sagas;

    class SagaPersister : ISagaPersister
    {
        public Task Save(IContainSagaData saga, SagaCorrelationProperty correlationProperty, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            return session.Session().SaveAsync(saga, cancellationToken);
        }

        public Task Update(IContainSagaData saga, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            return session.Session().UpdateAsync(saga, cancellationToken);
        }

        public Task<T> Get<T>(Guid sagaId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
            where T : class, IContainSagaData
        {
            return session.Session().GetAsync<T>(sagaId, GetLockModeForSaga<T>(), cancellationToken);
        }

        Task<T> ISagaPersister.Get<T>(string property, object value, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken)
        {
            return session.Session().CreateCriteria(typeof(T))
                .SetLockMode(GetLockModeForSaga<T>())
                .Add(Restrictions.Eq(property, value))
                .UniqueResultAsync<T>(cancellationToken);
        }

        public Task Complete(IContainSagaData saga, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            return session.Session().DeleteAsync(saga, cancellationToken);
        }

        static LockMode GetLockModeForSaga<T>()
        {
            var explicitLockModeAttribute = typeof(T).GetCustomAttributes(typeof(LockModeAttribute), false).SingleOrDefault();

            if (explicitLockModeAttribute == null)
            {
                return LockMode.Upgrade;//our new default in v4.1.0
            }

            var explicitLockMode = ((LockModeAttribute)explicitLockModeAttribute).RequestedLockMode;

            return explicitLockMode switch
            {
                LockModes.Force => LockMode.Force,
                LockModes.None => LockMode.None,
                LockModes.Read => LockMode.Read,
                LockModes.Upgrade => LockMode.Upgrade,
                LockModes.UpgradeNoWait => LockMode.UpgradeNoWait,
                LockModes.Write => LockMode.Write,
                _ => throw new InvalidOperationException("Unknown lock mode requested: " + explicitLockMode)
            };
        }
    }
}
