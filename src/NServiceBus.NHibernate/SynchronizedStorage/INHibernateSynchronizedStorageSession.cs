namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate;
    using NServiceBus.Persistence;

    interface INHibernateSynchronizedStorageSession
    {
        ISession Session { get; }

        void OnSaveChanges(Func<SynchronizedStorageSession, Task> callback);
    }
}