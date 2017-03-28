namespace NServiceBus.NHibernate.Tests.SynchronizedStorage
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.Persistence.NHibernate;
    using NUnit.Framework;

    [TestFixture]
    class NHibernateOutboxTransactionSynchronizedStorageSessionTests : InMemoryDBFixture
    {
        [Test]
        public async Task It_does_not_invoke_callbacks_when_session_is_completed_and_dispoased()
        {
            var session = SessionFactory.OpenSession();
            var transaction = session.BeginTransaction();

            var callbackInvoked = false;
            using (var outboxTransaction = new NHibernateOutboxTransaction(session, transaction))
            {
                var adapter = new NHibernateSynchronizedStorageAdapter(SessionFactory);

                using (var storageSession = await adapter.TryAdapt(outboxTransaction, new ContextBag()))
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
            var session = SessionFactory.OpenSession();
            var transaction = session.BeginTransaction();

            var callbackInvoked = false;
            using (var outboxTransaction = new NHibernateOutboxTransaction(session, transaction))
            {
                var adapter = new NHibernateSynchronizedStorageAdapter(SessionFactory);

                using (var storageSession = await adapter.TryAdapt(outboxTransaction, new ContextBag()))
                {
                    storageSession.Session(); //Make sure session is initialized
                    storageSession.OnSaveChanges(s =>
                    {
                        callbackInvoked = true;
                        return Task.FromResult(0);
                    });

                    await storageSession.CompleteAsync();
                }

                await outboxTransaction.Commit();
            }

            Assert.IsTrue(callbackInvoked);
        }

        [Test]
        public async Task It_does_not_commit_outbox_transaction_if_callback_throws()
        {
            var entityId = Guid.NewGuid().ToString();
            var exceptionThrown = false;
            using (var session = SessionFactory.OpenSession())
            {
                var transaction = session.BeginTransaction();

                using (var outboxTransaction = new NHibernateOutboxTransaction(session, transaction))
                {
                    var adapter = new NHibernateSynchronizedStorageAdapter(SessionFactory);

                    using (var storageSession = await adapter.TryAdapt(outboxTransaction, new ContextBag()))
                    {
                        storageSession.Session().Save(new TestEntity()
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
            }

            using (var session = SessionFactory.OpenSession())
            {
                var savedEntity = session.Get<TestEntity>(entityId);
                Assert.IsNull(savedEntity);
            }
        }
    }
}