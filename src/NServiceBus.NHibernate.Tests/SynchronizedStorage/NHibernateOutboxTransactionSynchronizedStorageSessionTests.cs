namespace NServiceBus.NHibernate.Tests.SynchronizedStorage
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NHibernate.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using Persistence.NHibernate;
    using NUnit.Framework;

    [TestFixture(false, false)]
    [TestFixture(true, false)]
    [TestFixture(false, true)]
    [TestFixture(true, true)]
    class NHibernateOutboxTransactionSynchronizedStorageSessionTests : InMemoryDBFixture
    {
        bool pessimistic;
        bool transactionScope;

        public NHibernateOutboxTransactionSynchronizedStorageSessionTests(bool pessimistic, bool transactionScope)
        {
            this.pessimistic = pessimistic;
            this.transactionScope = transactionScope;
        }

        [Test]
        public async Task It_does_not_invoke_callbacks_when_session_is_completed_and_disposed()
        {
            var callbackInvoked = false;

            var outboxPersisterFactory = new OutboxPersisterFactory<OutboxRecord>();
            var persister = outboxPersisterFactory.Create(SessionFactory, "TestEndpoint", pessimistic, transactionScope);
            
            var messageId = Guid.NewGuid().ToString("N");
            var contextBag = new ContextBag();
            await persister.Get(messageId, contextBag);

            using (var outboxTransaction = await persister.BeginTransaction(contextBag))
            {
                var adapter = new NHibernateSynchronizedStorageAdapter(SessionFactory, null);

                using (var storageSession = await adapter.TryAdapt(outboxTransaction, contextBag))
                {
                    storageSession.Session(); //Make sure session is initialized
                    storageSession.OnSaveChanges(s =>
                    {
                        callbackInvoked = true;
                        return Task.FromResult(0);
                    });

                    await storageSession.CompleteAsync();
                }

                Assert.IsFalse(callbackInvoked);
            }
        }

        [Test]
        public async Task It_invokes_callbacks_when_outbox_transaction_is_completed()
        {
            var callbackInvoked = 0;

            var outboxPersisterFactory = new OutboxPersisterFactory<OutboxRecord>();
            var persister = outboxPersisterFactory.Create(SessionFactory, "TestEndpoint", pessimistic, transactionScope);

            var messageId = Guid.NewGuid().ToString("N");
            var contextBag = new ContextBag();
            await persister.Get(messageId, contextBag);

            using (var outboxTransaction = await persister.BeginTransaction(contextBag))
            {
                var adapter = new NHibernateSynchronizedStorageAdapter(SessionFactory, null);

                using (var storageSession = await adapter.TryAdapt(outboxTransaction, contextBag))
                {
                    storageSession.Session(); //Make sure session is initialized
                    storageSession.OnSaveChanges(s =>
                    {
                        callbackInvoked++;
                        return Task.FromResult(0);
                    });
                    storageSession.OnSaveChanges(s =>
                    {
                        callbackInvoked++;
                        return Task.FromResult(0);
                    });

                    await storageSession.CompleteAsync();
                }

                await outboxTransaction.Commit();
            }

            Assert.AreEqual(2, callbackInvoked);
        }

        [Test]
        public async Task It_does_not_commit_outbox_transaction_if_callback_throws()
        {
            var entityId = Guid.NewGuid().ToString();
            var exceptionThrown = false;

            var outboxPersisterFactory = new OutboxPersisterFactory<OutboxRecord>();
            var persister = outboxPersisterFactory.Create(SessionFactory, "TestEndpoint", pessimistic, transactionScope);

            var messageId = Guid.NewGuid().ToString("N");
            var contextBag = new ContextBag();
            await persister.Get(messageId, contextBag);

            using (var outboxTransaction = await persister.BeginTransaction(contextBag))
            {
                var adapter = new NHibernateSynchronizedStorageAdapter(SessionFactory, null);

                using (var storageSession = await adapter.TryAdapt(outboxTransaction, contextBag))
                {
                    storageSession.Session().Save(new TestEntity
                    {
                        Id = entityId
                    });
                    storageSession.OnSaveChanges(s =>
                    {
                        throw new Exception("Simulated");
                    });

                    try
                    {
                        await storageSession.CompleteAsync();
                        await outboxTransaction.Commit();
                    }
                    catch (Exception)
                    {
                        exceptionThrown = true;
                    }
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