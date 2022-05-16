namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    interface INHibernateStorageSessionInternal : INHibernateStorageSession, IDisposable
    {
        Task CompleteAsync(CancellationToken cancellationToken = default);
    }
}