namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate;

    interface INHibernateSynchronizedStorageSession
    {
        ISession Session { get; }

        void OnSaveChanges(Func<Task> callback);
    }
}