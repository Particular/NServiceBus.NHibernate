namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data;
    using global::NHibernate;

    /// <summary>
    /// Provides users with access to the current NHibernate <see cref="ITransaction"/>, <see cref="IDbConnection"/> and <see cref="ISession"/>.
    /// </summary>
    [ObsoleteEx(
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7",
        ReplacementTypeOrMember = "IMessageHandlingContext.StorageSession")]
    public class NHibernateStorageContext
    {
        /// <summary>
        /// Gets the database connection associated with the current NHibernate <see cref="Session"/>
        /// </summary>
        public IDbConnection Connection
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the database transaction associated with the current NHibernate <see cref="Session"/> or null when using TransactionScope.
        /// </summary>
        public IDbTransaction DatabaseTransaction
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the current context NHibernate <see cref="ISession"/>.
        /// </summary>
        public ISession Session
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the current context NHibernate <see cref="ITransaction"/>.
        /// </summary>
        public ITransaction Transaction
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}