namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using NUnit.Framework;
    using SagaPersisters.NHibernate.Tests;

    [TestFixture]
    class When_adding_outbox_messages : InMemoryDBFixture
    {
        [Test]
        [ExpectedException]
        public void Should_throw_if__trying_to_insert_same_messageid()
        {
            using (var session = SessionFactory.OpenSession())
            {
                persister.StorageSessionProvider = new FakeSessionProvider(session);
                persister.Store("MySpecialId", Enumerable.Empty<TransportOperation>());
                persister.Store("MySpecialId", Enumerable.Empty<TransportOperation>());

                session.Flush();
            }
        }

        [Test]
        public void Should_save_with_not_dispatched()
        {
            var id = Guid.NewGuid().ToString("N");
            using (var session = SessionFactory.OpenSession())
            {
                persister.StorageSessionProvider = new FakeSessionProvider(session);
                persister.Store(id, new List<TransportOperation>
                {
                    new TransportOperation("MyMessage", new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
                });

                session.Flush();
            }

            OutboxMessage result;
            persister.TryGet(id, out result);


            var operation = result.TransportOperations.Single();


            Assert.AreEqual("MyMessage", operation.MessageId);
        }

        [Test]
        public void Should_update_dispatched_flag()
        {
            var id = Guid.NewGuid().ToString("N");

            using (var session = SessionFactory.OpenSession())
            {
                persister.StorageSessionProvider = new FakeSessionProvider(session);
                persister.Store(id, new List<TransportOperation>
                {
                    new TransportOperation("MyMessage", new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
                });

                session.Flush();
            }

            persister.SetAsDispatched(id);

            using (var session = SessionFactory.OpenSession())
            {
                var result = session.QueryOver<OutboxRecord>().Where(o => o.MessageId == id)
                    .SingleOrDefault();


                Assert.True(result.Dispatched);
            }
        }

        [Test]
        [ExpectedException(typeof(Exception))]
        public void Should_throw_concurrency_exception_if_dispatched_flag_has_already_been_set()
        {
            var id = Guid.NewGuid().ToString("N");

            using (var session = SessionFactory.OpenSession())
            {
                persister.StorageSessionProvider = new FakeSessionProvider(session);
                persister.Store(id, new List<TransportOperation>
                {
                    new TransportOperation("MyMessage", new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
                });

                session.Flush();
            }

            persister.SetAsDispatched(id);
            persister.SetAsDispatched(id);
        }
    }
}