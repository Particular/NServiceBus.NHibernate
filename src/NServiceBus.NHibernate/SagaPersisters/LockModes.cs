namespace NServiceBus.SagaPersisters.NHibernate
{
    /// <summary>
    /// The <see cref="LockModes"/> class defines the different lock levels that may be acquired by NHibernate.
    /// </summary>
    public enum LockModes
    {
        /// <summary>
        /// No lock required. 
        /// </summary>
        /// <remarks>
        /// If an object is requested with this lock mode, a <c>Read</c> lock
        /// might be obtained if necessary.
        /// </remarks>
        None = 1,

        /// <summary>
        /// A shared lock. 
        /// </summary>
        /// <remarks>
        /// Objects are loaded in <c>Read</c> mode by default
        /// </remarks>
        Read = 2,

        /// <summary>
        /// An upgrade lock. 
        /// </summary>
        /// <remarks>
        /// Objects loaded in this lock mode are materialized using an
        /// SQL <c>SELECT ... FOR UPDATE</c>
        /// </remarks>
        Upgrade = 3,

        /// <summary>
        /// Attempt to obtain an upgrade lock, using an Oracle-style
        /// <c>SELECT ... FOR UPGRADE NOWAIT</c>. 
        /// </summary>
        /// <remarks>
        /// The semantics of this lock mode, once obtained, are the same as <c>Upgrade</c>
        /// </remarks>
        UpgradeNoWait = 4,

        /// <summary>
        /// A <c>Write</c> lock is obtained when an object is updated or inserted.
        /// </summary>
        /// <remarks>
        /// This is not a valid mode for <c>Load()</c> or <c>Lock()</c>.
        /// </remarks>
        Write = 5,

        /// <summary> 
        /// Similar to <see cref="Upgrade"/> except that, for versioned entities,
        /// it results in a forced version increment.
        /// </summary>
        Force = 6
    }
}