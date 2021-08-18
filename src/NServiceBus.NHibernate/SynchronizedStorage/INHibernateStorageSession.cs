namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Persistence;

    /// <summary>
    /// Exposes the current <see cref="ISession"/> managed by NServiceBus.
    /// </summary>
    public interface INHibernateStorageSession
    {
        /// <summary>
        /// Gets the session.
        /// </summary>
        ISession Session { get; }

        /// <summary>
        /// Registers a callback to be executed before the storage session changes are committed.
        /// </summary>
        void OnSaveChanges(Func<ISynchronizedStorageSession, CancellationToken, Task> callback);

        /// <summary>
        /// Registers a callback to be executed before the storage session changes are committed.
        /// </summary>
        [ObsoleteEx(Message = "Use the overload that supports cancellation.", RemoveInVersion = "10", TreatAsErrorFromVersion = "9")]
#pragma warning disable PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
        void OnSaveChanges(Func<ISynchronizedStorageSession, Task> callback);
#pragma warning restore PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
    }
}