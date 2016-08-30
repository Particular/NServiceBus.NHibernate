namespace NServiceBus.Testing
{
    using global::NHibernate;
    using Janitor;
    using NServiceBus.Persistence;

    /// <summary>
    /// Allows writing automated tests against handlers which use NServiceBus-managed NHibernate session.
    /// </summary>
    [SkipWeaving]
    public class TestingNHibernateSynchronizedStorageSession: SynchronizedStorageSession, INHibernateSynchronizedStorageSession
    {
        /// <summary>
        /// Creates new instance of the session.
        /// </summary>
        /// <param name="session">An opened NHibernate session to use in the test. The session is not automatically flushed..</param>
        public TestingNHibernateSynchronizedStorageSession(ISession session)
        {
            Session = session;
        }

        /// <summary>
        /// Gets the underlying NHibernate session.
        /// </summary>
        public ISession Session { get; }
    }
}