namespace NServiceBus.NHibernate.Tests.SynchronizedStorage
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.NHibernate;
    using NServiceBus.Transport;
    using NUnit.Framework;

    [TestFixture]
    class NHibernateLazyNativeTransactionSynchronizedStorageSession : InMemoryDBFixture
    {
        [Test]
        public async Task It_invokes_callbacks_when_session_is_completed()
        {
            var storage = new NHibernateSynchronizedStorage(SessionFactory);

            var callbackInvoked = false;

            using (var storageSession = await storage.OpenSession(new ContextBag()))
            {
                storageSession.Session(); //Make sure session is initialized
                storageSession.OnSaveChanges(s =>
                {
                    callbackInvoked = true;
                    return Task.FromResult(0);
                });

                await storageSession.CompleteAsync();

                Assert.IsTrue(callbackInvoked);
            }
        }

        [Test]
        public async Task It_does_not_commit_if_callback_throws()
        {
            var entityId = Guid.NewGuid().ToString();
            var exceptionThrown = false;
            var storage = new NHibernateSynchronizedStorage(SessionFactory);

            using (var storageSession = await storage.OpenSession(new ContextBag()))
            {
                storageSession.Session().Save(new TestEntity() { Id = entityId });
                storageSession.OnSaveChanges(s =>
                {
                    throw new Exception("Simulated");
                });

                try
                {
                    await storageSession.CompleteAsync();
                }
                catch (Exception)
                {
                    exceptionThrown = true;
                }
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