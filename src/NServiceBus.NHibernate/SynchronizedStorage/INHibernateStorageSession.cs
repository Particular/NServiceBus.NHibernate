﻿namespace NServiceBus
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
        void OnSaveChanges(Func<SynchronizedStorageSession, CancellationToken, Task> callback);

        /// <summary>
        /// Registers a callback to be executed before the storage session changes are committed.
        /// </summary>
        [ObsoleteEx(Message = "Use the overload that supports cancellation.", RemoveInVersion = "10", TreatAsErrorFromVersion = "9")]
        void OnSaveChanges(Func<SynchronizedStorageSession, Task> callback);
    }
}