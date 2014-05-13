namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Outbox;
    using NUnit.Framework;
    using Persistence;
    using SagaPersisters.NHibernate.Tests;
    using Unicast;

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
                    new TransportOperation(new SendOptions("Foo@Machine")
                    {
                        CorrelationId = "MySpecialId3",
                        DelayDeliveryWith = TimeSpan.FromDays(34),
                        DeliverAt = DateTime.Now.AddHours(2),
                        Intent = MessageIntentEnum.Reply,
                        ReplyToAddress = new Address("Foo2", "Machine2"),
                    }, new TransportMessage
                    {
                        Body = new byte[1024*5]
                    }, "MyMessage"),
                });

                session.Flush();
            }

            OutboxMessage result;
            persister.TryGet(id, out result);

            Assert.IsFalse(result.Dispatched);

            var operation = result.TransportOperations.Single();


            Assert.AreEqual("MyMessage", operation.MessageType);
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
                    new TransportOperation(new SendOptions("Foo@Machine")
                    {
                        CorrelationId = "MySpecialId3",
                        DelayDeliveryWith = TimeSpan.FromDays(34),
                        DeliverAt = DateTime.Now.AddHours(2),
                        Intent = MessageIntentEnum.Reply,
                        ReplyToAddress = new Address("Foo2", "Machine2"),
                    }, new TransportMessage
                    {
                        Body = new byte[1024*5]
                    }, "MyMessage"),
                });

                session.Flush();
            }

            persister.SetAsDispatched(id);

            OutboxMessage result;
            persister.TryGet(id, out result);

            Assert.IsTrue(result.Dispatched);
        }

        [Test]
        [ExpectedException(typeof(ConcurrencyException))]
        public void Should_throw_concurrency_exception_if_dispatched_flag_has_already_been_set()
        {
            var id = Guid.NewGuid().ToString("N");

            using (var session = SessionFactory.OpenSession())
            {
                persister.StorageSessionProvider = new FakeSessionProvider(session);
                persister.Store(id, new List<TransportOperation>
                {
                    new TransportOperation(new SendOptions("Foo@Machine")
                    {
                        CorrelationId = "MySpecialId3",
                        DelayDeliveryWith = TimeSpan.FromDays(34),
                        DeliverAt = DateTime.Now.AddHours(2),
                        Intent = MessageIntentEnum.Reply,
                        ReplyToAddress = new Address("Foo2", "Machine2"),
                    }, new TransportMessage
                    {
                        Body = new byte[1024*5]
                    }, "MyMessage"),
                });

                session.Flush();
            }

            persister.SetAsDispatched(id);
            persister.SetAsDispatched(id);
        }
    }
}