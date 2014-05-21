namespace NServiceBus.SagaPersisters.NHibernate
{
    using System;

    /// <summary>
    /// Specifies the lock mode to use by default while retrieving <see cref="Saga"/> data.
    /// </summary>
    /// /// <remarks>
    /// It is not intended that users spend much time worrying about locking since Hibernate
    /// usually obtains exactly the right lock level automatically. Some "advanced" users may
    /// wish to explicitly specify lock levels.
    /// If not specified we use <see cref="LockModes.Upgrade"/>.
    /// For more information about lock modes see http://www.nhforge.org/doc/nh/en/#transactions-locking
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class LockModeAttribute : Attribute
    {
        /// <summary>
        /// Gets the <see cref="LockModes"/> to be used by the framework while retrieving <see cref="Saga"/> data.
        /// </summary>
        public LockModes RequestedLockMode { get; private set; }

        /// <summary>
        /// Create a new instance of <see cref="LockModeAttribute"/>.
        /// </summary>
        /// <param name="lockModeToUse">The <see cref="LockModes"/> to be used by the framework while retrieving <see cref="Saga"/> data.</param>
        public LockModeAttribute(LockModes lockModeToUse)
        {
            RequestedLockMode = lockModeToUse;
        }
    }
}