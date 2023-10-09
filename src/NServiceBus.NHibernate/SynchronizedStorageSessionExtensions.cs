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
            if (session is INHibernateStorageSessionProvider sessionProvider)
            {
                return sessionProvider.InternalSession.Session;
            }
            throw new InvalidOperationException("Shared session has not been configured for NHibernate.");
        }

        internal static INHibernateStorageSession StorageSession(this ISynchronizedStorageSession session)
        {
            if (session is INHibernateStorageSessionProvider sessionProvider)
            {
                return sessionProvider.InternalSession;
            }
            throw new InvalidOperationException("Shared session has not been configured for NHibernate.");
        }

        /// <summary>
        /// Registers a callback to be called before completing the session.
        /// </summary>
        public static void OnSaveChanges(this ISynchronizedStorageSession session, Func<ISynchronizedStorageSession, CancellationToken, Task> callback)
        {
            if (session is INHibernateStorageSessionProvider sessionProvider)
            {
                sessionProvider.InternalSession.OnSaveChanges(callback);
            }
            else
            {
                throw new InvalidOperationException("Shared session has not been configured for NHibernate.");
            }
        }
    }
}