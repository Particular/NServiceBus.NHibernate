namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Persistence;
    using Persistence.NHibernate;

    /// <summary>
    /// Shared session extensions for NHibernate persistence.
    /// </summary>
    public static class SynchronizedStorageSessionExtensions
    {
        /// <summary>
        /// Gets the current context NHibernate <see cref="ISession"/>.
        /// </summary>
        public static ISession Session(this ISynchronizedStorageSession session)
        {
            if (session is NHibernateSynchronizedStorageSession ambientTransactionSession)
            {
                return ambientTransactionSession.Session.Session;
            }
            throw new InvalidOperationException("Shared session has not been configured for NHibernate.");
        }

        internal static INHibernateStorageSession StorageSession(this ISynchronizedStorageSession session)
        {
            if (session is NHibernateSynchronizedStorageSession ambientTransactionSession)
            {
                return ambientTransactionSession.Session;
            }
            throw new InvalidOperationException("Shared session has not been configured for NHibernate.");
        }

        /// <summary>
        /// Registers a callback to be called before completing the session.
        /// </summary>
        public static void OnSaveChanges(this ISynchronizedStorageSession session, Func<ISynchronizedStorageSession, CancellationToken, Task> callback)
        {
            if (session is NHibernateSynchronizedStorageSession nhibernateSession)
            {
                nhibernateSession.Session.OnSaveChanges(callback);
            }
            else
            {
                throw new InvalidOperationException("Shared session has not been configured for NHibernate.");
            }
        }

        /// <summary>
        /// Registers a callback to be called before completing the session.
        /// </summary>
        [ObsoleteEx(Message = "Use the overload that supports cancellation.", RemoveInVersion = "10", TreatAsErrorFromVersion = "9")]
#pragma warning disable PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
        public static void OnSaveChanges(this ISynchronizedStorageSession session, Func<ISynchronizedStorageSession, Task> callback)
#pragma warning restore PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
        {
            OnSaveChanges(session, (s, _) => callback(s));
        }
    }
}