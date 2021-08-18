namespace NServiceBus.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;
    using Persistence;

    /// <summary>
    /// Allows writing automated tests against handlers which use NServiceBus-managed NHibernate session.
    /// </summary>
    [SkipWeaving]
    public class TestingNHibernateSynchronizedStorageSession : ISynchronizedStorageSession, INHibernateStorageSession
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

        void INHibernateStorageSession.OnSaveChanges(Func<ISynchronizedStorageSession, CancellationToken, Task> callback)
        {
            //NOOP
        }


        [ObsoleteEx(Message = "Use the overload that supports cancellation.", RemoveInVersion = "10", TreatAsErrorFromVersion = "9")]
        void INHibernateStorageSession.OnSaveChanges(Func<ISynchronizedStorageSession, Task> callback)
        {
            throw new NotImplementedException();
        }
    }
}