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
    class NHibernateAmbientTransactionSynchronizedStorageSessionTests : InMemoryDBFixture
    {
        [Test]
        public async Task It_invokes_callbacks_when_session_is_completed()
        {
            using (var scope = new TransactionScope())
            {
                var transportTransaction = new TransportTransaction();
                transportTransaction.Set(Transaction.Current);

                var callbackInvoked = false;
                var adapter = new NHibernateSynchronizedStorageAdapter(SessionFactory);

                using (var storageSession = await adapter.TryAdapt(transportTransaction, new ContextBag()))
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

                scope.Complete();
            }
        }

        [Test]
        public async Task It_does_not_commit_if_callback_throws()
        {
            var entityId = Guid.NewGuid().ToString();

            using (var scope = new TransactionScope())
            {
                var transportTransaction = new TransportTransaction();
                transportTransaction.Set(Transaction.Current);

                var adapter = new NHibernateSynchronizedStorageAdapter(SessionFactory);

                using (var storageSession = await adapter.TryAdapt(transportTransaction, new ContextBag()))
                {
                    storageSession.Session().Save(new TestEntity() {Id = entityId});
                    storageSession.OnSaveChanges(s =>
                    {
                        throw new Exception("Simulated");
                    });

                    try
                    {
                        await storageSession.CompleteAsync();
                        scope.Complete();
                    }
                    catch (Exception)
                    {
                        //NOOP
                    }
                }

            }

            using (var session = SessionFactory.OpenSession())
            {
                var savedEntity = session.Get<TestEntity>(entityId);
                Assert.IsNull(savedEntity);
            }
        }
    }
}