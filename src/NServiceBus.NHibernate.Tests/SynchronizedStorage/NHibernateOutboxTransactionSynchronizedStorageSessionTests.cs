namespace NServiceBus.NHibernate.Tests.SynchronizedStorage
{
    using System;
    using System.Data;
    using System.Threading.Tasks;
    using NHibernate.SynchronizedStorage;
    using NServiceBus.Extensibility;
    using NServiceBus.NHibernate.Outbox;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.Persistence.NHibernate;
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
            var persister = outboxPersisterFactory.Create(SessionFactory, "TestEndpoint", pessimistic, transactionScope, IsolationLevel.ReadCommitted, System.Transactions.IsolationLevel.ReadCommitted);

            var messageId = Guid.NewGuid().ToString("N");
            var contextBag = new ContextBag();
            await persister.Get(messageId, contextBag);

            using (var outboxTransaction = await persister.BeginTransaction(contextBag))
            {
                var storageSession = await OpenStorageSession(outboxTransaction);
                storageSession.OnSaveChanges((s, _) =>
                {
                    callbackInvoked = true;
                    return Task.CompletedTask;
                });

                await storageSession.CompleteAsync();

                Assert.That(callbackInvoked, Is.False);
            }
        }

        [Test]
        public async Task It_invokes_callbacks_when_outbox_transaction_is_completed()
        {
            var callbackInvoked = 0;

            var outboxPersisterFactory = new OutboxPersisterFactory<OutboxRecord>();
            var persister = outboxPersisterFactory.Create(SessionFactory, "TestEndpoint", pessimistic, transactionScope, IsolationLevel.ReadCommitted, System.Transactions.IsolationLevel.ReadCommitted);

            var messageId = Guid.NewGuid().ToString("N");
            var contextBag = new ContextBag();
            await persister.Get(messageId, contextBag);

            using (var outboxTransaction = await persister.BeginTransaction(contextBag))
            {
                var storageSession = await OpenStorageSession(outboxTransaction);

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

                await storageSession.CompleteAsync();


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
            var persister = outboxPersisterFactory.Create(SessionFactory, "TestEndpoint", pessimistic, transactionScope, IsolationLevel.ReadCommitted, System.Transactions.IsolationLevel.ReadCommitted);

            var messageId = Guid.NewGuid().ToString("N");
            var contextBag = new ContextBag();
            await persister.Get(messageId, contextBag);

            using (var outboxTransaction = await persister.BeginTransaction(contextBag))
            {
                var storageSession = await OpenStorageSession(outboxTransaction);

                storageSession.InternalSession.Session.Save(new TestEntity
                {
                    Id = entityId
                });
                storageSession.OnSaveChanges((s, _) =>
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
            Assert.IsTrue(exceptionThrown);

            using (var session = SessionFactory.OpenSession())
            {
                var savedEntity = session.Get<TestEntity>(entityId);
                Assert.IsNull(savedEntity);
            }
        }

        async Task<NHibernateSynchronizedStorageSession> OpenStorageSession(IOutboxTransaction outboxTransaction)
        {
            //The open method creates the NHibernateLazyNativeTransactionSynchronizedStorageSession
            var syncSession = new NHibernateSynchronizedStorageSession(new SessionFactoryHolder(SessionFactory));
            var success = await syncSession.TryOpen(outboxTransaction, new ContextBag());
            Assert.IsTrue(success);
            var storageSession = syncSession.InternalSession;

            var _ = storageSession.Session; //Make sure session is initialized
            return syncSession;
        }
    }
}