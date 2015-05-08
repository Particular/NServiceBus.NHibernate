namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.Persistence.NHibernate;
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
                persister.StorageSessionProvider = new FakeSessionProvider(SessionFactory, session);
                persister.SessionFactoryProvider = new SessionFactoryProvider(SessionFactory);
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
                persister.StorageSessionProvider = new FakeSessionProvider(SessionFactory, session);
                persister.SessionFactoryProvider = new SessionFactoryProvider(SessionFactory);
                persister.Store(id, new List<TransportOperation>
                {
                    new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
                });

                session.Flush();
            }

            OutboxMessage result;
            persister.TryGet(id, out result);

            var operation = result.TransportOperations.Single();

            Assert.AreEqual(id, operation.MessageId);
        }

        [Test]
        public void Should_update_dispatched_flag()
        {
            var id = Guid.NewGuid().ToString("N");

            using (var session = SessionFactory.OpenSession())
            {
                persister.StorageSessionProvider = new FakeSessionProvider(SessionFactory, session);
                persister.SessionFactoryProvider = new SessionFactoryProvider(SessionFactory);
                persister.Store(id, new List<TransportOperation>
                {
                    new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
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
        public void Should_delete_all_OutboxRecords_that_have_been_dispatched()
        {
            var id = Guid.NewGuid().ToString("N");
            
            using (var session = SessionFactory.OpenSession())
            {
                persister.StorageSessionProvider = new FakeSessionProvider(SessionFactory, session);
                persister.SessionFactoryProvider = new SessionFactoryProvider(SessionFactory);
                persister.Store("NotDispatched", Enumerable.Empty<TransportOperation>());
                persister.Store(id, new List<TransportOperation>
                {
                    new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
                });

                session.Flush();
            }

            persister.SetAsDispatched(id);
            Thread.Sleep(TimeSpan.FromSeconds(1)); //Need to wait for dispatch logic to finish

            persister.RemoveEntriesOlderThan(DateTime.UtcNow.AddMinutes(1));

            using (var session = SessionFactory.OpenSession())
            {
                var result = session.QueryOver<OutboxRecord>().List();
                    
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("NotDispatched", result[0].MessageId);
            }
        }
    }
}