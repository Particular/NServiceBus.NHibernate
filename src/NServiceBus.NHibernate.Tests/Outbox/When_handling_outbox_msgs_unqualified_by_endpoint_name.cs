namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System;
    using System.Linq;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.Persistence.NHibernate;
    using NUnit.Framework;
    using SagaPersisters.NHibernate.Tests;

    [TestFixture]
    class When_handling_outbox_msgs_unqualified_by_endpoint_name : InMemoryDBFixture
    {
        Guid blar = Guid.NewGuid();

        [Test]
        public void Should_find_record_with_qualified_or_unqualified_message_id()
        {
            Console.WriteLine(blar);

            var raw1 = Guid.NewGuid().ToString("N");
            var raw2 = Guid.NewGuid().ToString("N");

            var qualified2 = "TestEndpoint/" + raw2;

            using (var session = SessionFactory.OpenSession())
            {
                session.Save(new OutboxRecord { MessageId = raw1 });
                session.Save(new OutboxRecord { MessageId = qualified2 });

                session.Flush();
            }
            
            OutboxMessage fetch1;
            OutboxMessage fetch2;

            using (var session = SessionFactory.OpenSession())
            {
                persister.StorageSessionProvider = new FakeSessionProvider(SessionFactory, session);
                persister.SessionFactoryProvider = new SessionFactoryProvider(SessionFactory);

                
                persister.TryGet(raw1, out fetch1);
                persister.TryGet(raw2, out fetch2);
            }

            Assert.NotNull(fetch1);
            Assert.NotNull(fetch2);
            Assert.AreEqual(raw1, fetch1.MessageId);
            Assert.AreEqual(qualified2, fetch2.MessageId);
        }

        [Test]
        public void Should_qualify_message_id_on_store()
        {
            Console.WriteLine(blar);

            string id = Guid.NewGuid().ToString("N");
            string qualified = "TestEndpoint/" + id;

            using (var session = SessionFactory.OpenSession())
            {
                persister.StorageSessionProvider = new FakeSessionProvider(SessionFactory, session);
                persister.SessionFactoryProvider = new SessionFactoryProvider(SessionFactory);

                persister.Store(id, new TransportOperation[0]);
                session.Flush();
            }

            using (var session = SessionFactory.OpenSession())
            {
                var result = session.QueryOver<OutboxRecord>().List();

                Assert.AreEqual(1, result.Count);
                Assert.AreEqual(qualified, result[0].MessageId);
            }
        }

        [Test]
        public void Should_mark_dispatched_for_unqualified_message_id()
        {
            Console.WriteLine(blar);

            string[] set1 = Enumerable.Range(0, 10).Select(x => Guid.NewGuid().ToString("N")).ToArray();
            string[] set2 = Enumerable.Range(0, 10).Select(x => Guid.NewGuid().ToString("N")).ToArray();

            using (var session = SessionFactory.OpenSession())
            {
                // Store by only unqualified msg id for evens, qualify with endpoint name for odds 
                for (var i=0; i<10; i++)
                {
                    // These will get dispatched in the first round
                    string persistedId1 = (i%2 == 0) ? set1[i] : "TestEndpoint/" + set1[i];
                    session.Save(new OutboxRecord { MessageId = persistedId1 });

                    // These will get dispatched in the second round
                    string persistedId2 = (i % 2 == 0) ? set2[i] : "TestEndpoint/" + set2[i];
                    session.Save(new OutboxRecord { MessageId = persistedId2 });
                }

                session.Flush();
            }

            using (var session = SessionFactory.OpenSession())
            {
                persister.StorageSessionProvider = new FakeSessionProvider(SessionFactory, session);
                persister.SessionFactoryProvider = new SessionFactoryProvider(SessionFactory);

                foreach (var id in set1)
                    persister.SetAsDispatched(id);

                session.Flush();
            }

            using (var session = SessionFactory.OpenSession())
            {
                var result = session.QueryOver<OutboxRecord>().List();

                Assert.AreEqual(20, result.Count);
                Assert.AreEqual(10, result.Count(rec => rec.Dispatched));
            }

            using (var session = SessionFactory.OpenSession())
            {
                persister.StorageSessionProvider = new FakeSessionProvider(SessionFactory, session);
                persister.SessionFactoryProvider = new SessionFactoryProvider(SessionFactory);

                foreach (var id in set2)
                    persister.SetAsDispatched(id);

                session.Flush();
            }

            using (var session = SessionFactory.OpenSession())
            {
                var result = session.QueryOver<OutboxRecord>().List();

                Assert.AreEqual(20, result.Count);
                Assert.AreEqual(20, result.Count(rec => rec.Dispatched));
            }
        }
    }
}