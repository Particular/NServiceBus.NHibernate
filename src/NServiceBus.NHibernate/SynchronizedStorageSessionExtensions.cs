namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate;
    using NServiceBus.Persistence;

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
            var ambientTransactionSession = session as INHibernateSynchronizedStorageSession;
            if (ambientTransactionSession != null)
            {
                return ambientTransactionSession.Session;
            }
            throw new InvalidOperationException("Shared session has not been configured for NHibernate.");
        }

        /// <summary>
        /// Registers a callback to be called before completing the session.
        /// </summary>
        public static void OnSaveChanges(this SynchronizedStorageSession session, Func<Task> callback)
        {
            var nhibernateSession = session as INHibernateSynchronizedStorageSession;
            if (nhibernateSession != null)
            {
                nhibernateSession.OnSaveChanges(callback);
            }
            else
            {
                throw new InvalidOperationException("Shared session has not been configured for NHibernate.");
            }
        }
    }
}