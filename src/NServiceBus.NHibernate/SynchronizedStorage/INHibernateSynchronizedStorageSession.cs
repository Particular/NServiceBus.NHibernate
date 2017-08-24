namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Persistence;

    interface INHibernateSynchronizedStorageSession
    {
        ISession Session { get; }

        void OnSaveChanges(Func<SynchronizedStorageSession, Task> callback);
    }
}