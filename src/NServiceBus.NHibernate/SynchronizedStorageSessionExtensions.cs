﻿namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Persistence;

    /// <summary>
    /// Shared session extensions for NHibernate persistence.
    /// </summary>
    public static class SynchronizedStorageSessionExtensions
    {
        /// <summary>
        /// Gets the current context NHibernate <see cref="ISession"/>.
        /// </summary>
        public static ISession Session(this SynchronizedStorageSession session)
        {
            if (session is INHibernateStorageSession ambientTransactionSession)
            {
                return ambientTransactionSession.Session;
            }
            throw new InvalidOperationException("Shared session has not been configured for NHibernate.");
        }

        /// <summary>
        /// Registers a callback to be called before completing the session.
        /// </summary>
        public static void OnSaveChanges(this SynchronizedStorageSession session, Func<SynchronizedStorageSession, CancellationToken, Task> callback)
        {
            if (session is INHibernateStorageSession nhibernateSession)
            {
                nhibernateSession.OnSaveChanges(callback);
            }
            else
            {
                throw new InvalidOperationException("Shared session has not been configured for NHibernate.");
            }
        }

        /// <summary>
        /// Registers a callback to be called before completing the session.
        /// </summary>
        [ObsoleteEx(Message = "Use the overload that supports cancellation.", TreatAsErrorFromVersion = "10", RemoveInVersion = "11")]
        public static void OnSaveChanges(this SynchronizedStorageSession session, Func<SynchronizedStorageSession, Task> callback)
        {
            OnSaveChanges(session, (s, _) => callback(s));
        }
    }
}