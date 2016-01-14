namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using NUnit.Framework;

    [TestFixture]
    class When_adding_outbox_messages : InMemoryDBFixture
    {
        [Test]
        public async Task Should_throw_if_trying_to_insert_same_messageid()
        {
            var failed = false;
            using (var session = SessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                await persister.Store(new OutboxMessage("MySpecialId", new List<TransportOperation>()), new NHibernateOutboxTransaction(session, transaction), new ContextBag());
                transaction.Commit();
            }
            try
            {
                using (var session = SessionFactory.OpenSession())
                using (var transaction = session.BeginTransaction())
                {
                    await persister.Store(new OutboxMessage("MySpecialId", new List<TransportOperation>()), new NHibernateOutboxTransaction(session, transaction), new ContextBag());
                    transaction.Commit();
                }
            }
            catch (Exception)
            {
                failed = true;
            }
            Assert.IsTrue(failed);
        }

        [Test]
        public async Task Should_save_with_not_dispatched()
        {
            var id = Guid.NewGuid().ToString("N");
            using (var session = SessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                await persister.Store(new OutboxMessage(id, new List<TransportOperation>
                {
                    new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
                }), new NHibernateOutboxTransaction(session, transaction), new ContextBag());
                transaction.Commit();
            }

            var result = await persister.Get(id, new ContextBag());
            var operation = result.TransportOperations.Single();

            Assert.AreEqual(id, operation.MessageId);
        }

        [Test]
        public async Task Should_update_dispatched_flag()
        {
            var id = Guid.NewGuid().ToString("N");

            using (var session = SessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                await persister.Store(new OutboxMessage(id, new List<TransportOperation>
                {
                    new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
                }), new NHibernateOutboxTransaction(session, transaction), new ContextBag());

                transaction.Commit();
            }

            await persister.SetAsDispatched(id, new ContextBag());

            using (var session = SessionFactory.OpenSession())
            {
                var result = session.QueryOver<OutboxRecord>().Where(o => o.MessageId == "TestEndpoint/" + id)
                    .SingleOrDefault();

                Assert.True(result.Dispatched);
            }
        }

        [Test]
        public async Task Should_delete_all_OutboxRecords_that_have_been_dispatched()
        {
            var id = Guid.NewGuid().ToString("N");

            using (var session = SessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                await persister.Store(new OutboxMessage("NotDispatched", new List<TransportOperation>()), new NHibernateOutboxTransaction(session, transaction), new ContextBag());
                await persister.Store(new OutboxMessage(id, new List<TransportOperation>
                {
                    new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
                }), new NHibernateOutboxTransaction(session, transaction), new ContextBag());

                transaction.Commit();
            }

            await persister.SetAsDispatched(id, new ContextBag());

            persister.RemoveEntriesOlderThan(DateTime.UtcNow.AddMinutes(1));

            using (var session = SessionFactory.OpenSession())
            {
                var result = session.QueryOver<OutboxRecord>().List();

                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("TestEndpoint/NotDispatched", result[0].MessageId);
            }
        }
    }
}