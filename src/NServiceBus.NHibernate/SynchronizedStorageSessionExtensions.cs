namespace NServiceBus
{
    using System;
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
    }
}