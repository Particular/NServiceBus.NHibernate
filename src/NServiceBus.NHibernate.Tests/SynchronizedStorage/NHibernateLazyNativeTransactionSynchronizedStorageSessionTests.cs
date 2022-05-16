namespace NServiceBus.NHibernate.Tests.SynchronizedStorage
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NHibernate.SynchronizedStorage;
    using NUnit.Framework;
    using Persistence.NHibernate;

    [TestFixture]
    class NHibernateLazyNativeTransactionSynchronizedStorageSession : InMemoryDBFixture
    {
        [Test]
        public async Task It_invokes_callbacks_when_session_is_completed()
        {
            //The open method creates the NHibernateLazyNativeTransactionSynchronizedStorageSession
            var syncSession = new NHibernateSynchronizedStorageSession(new SessionFactoryHolder(SessionFactory));
            await syncSession.Open(new ContextBag());
            var storageSession = syncSession.InternalSession;

            var callbackInvoked = 0;

            var __ = storageSession.Session; //Make sure session is initialized
            storageSession.OnSaveChanges((s, _) =>
            {
                callbackInvoked++;
                return Task.CompletedTask;
            });
            storageSession.OnSaveChanges((s, _) =>
            {
                callbackInvoked++;
                return Task.CompletedTask;
            });

            await syncSession.CompleteAsync();

            Assert.AreEqual(2, callbackInvoked);
        }

        [Test]
        public async Task It_does_not_commit_if_callback_throws()
        {
            //The open method creates the NHibernateLazyNativeTransactionSynchronizedStorageSession
            var syncSession = new NHibernateSynchronizedStorageSession(new SessionFactoryHolder(SessionFactory));
            await syncSession.Open(new ContextBag());
            var storageSession = syncSession.InternalSession;

            var entityId = Guid.NewGuid().ToString();
            var exceptionThrown = false;

            storageSession.Session.Save(new TestEntity { Id = entityId });
            storageSession.OnSaveChanges((s, _) =>
            {
                throw new Exception("Simulated");
            });

            try
            {
                await syncSession.CompleteAsync();
            }
            catch (Exception)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown);

            using (var session = SessionFactory.OpenSession())
            {
                var savedEntity = session.Get<TestEntity>(entityId);
                Assert.IsNull(savedEntity);
            }
        }
    }
}