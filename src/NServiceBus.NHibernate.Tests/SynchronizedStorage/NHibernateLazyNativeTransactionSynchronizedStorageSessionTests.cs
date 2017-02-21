namespace NServiceBus.NHibernate.Tests.SynchronizedStorage
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.NHibernate;
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
                storageSession.RegisterCommitHook(() =>
                {
                    callbackInvoked = true;
                    return Task.FromResult(0);
                });

                await storageSession.CompleteAsync();

                Assert.IsTrue(callbackInvoked);
            }
        }
    }
}