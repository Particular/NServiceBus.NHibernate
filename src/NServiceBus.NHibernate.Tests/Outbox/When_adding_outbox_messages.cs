namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Outbox;
    using NUnit.Framework;
    using Persistence;
    using Unicast;

    [TestFixture]
    class When_adding_outbox_messages : InMemoryDBFixture
    {
        [Test]
        [ExpectedException]
        public void Should_throw_if__trying_to_insert_same_messageid()
        {
            persister.StoreAndCommit("MySpecialId", Enumerable.Empty<TransportOperation>());
            persister.StoreAndCommit("MySpecialId", Enumerable.Empty<TransportOperation>());
        }

        [Test]
        public void Should_save_with_not_dispatched()
        {
            var id = Guid.NewGuid().ToString("N");

            persister.StoreAndCommit(id, new List<TransportOperation>
                {
                    new TransportOperation(new SendOptions("Foo@Machine")
                    {
                        CorrelationId = "MySpecialId3",
                        DelayDeliveryWith = TimeSpan.FromDays(34),
                        DeliverAt = DateTime.Now.AddHours(2),
                        Intent = MessageIntentEnum.Reply,
                        ReplyToAddress = new Address("Foo2","Machine2"),
                    }, new TransportMessage
                    {
                        Body = new byte[1024*5]
                    },"MyMessage"),
                });

            OutboxMessage result;
            persister.TryGet(id, out result);

            Assert.IsFalse(result.Dispatched);
        }

        [Test]
        public void Should_update_dispatched_flag()
        {
            var id = Guid.NewGuid().ToString("N");

            persister.StoreAndCommit(id, new List<TransportOperation>
                {
                    new TransportOperation(new SendOptions("Foo@Machine")
                    {
                        CorrelationId = "MySpecialId3",
                        DelayDeliveryWith = TimeSpan.FromDays(34),
                        DeliverAt = DateTime.Now.AddHours(2),
                        Intent = MessageIntentEnum.Reply,
                        ReplyToAddress = new Address("Foo2","Machine2"),
                    }, new TransportMessage
                    {
                        Body = new byte[1024*5]
                    },"MyMessage"),
                });

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

            persister.StoreAndCommit(id, new List<TransportOperation>
                {
                    new TransportOperation(new SendOptions("Foo@Machine")
                    {
                        CorrelationId = "MySpecialId3",
                        DelayDeliveryWith = TimeSpan.FromDays(34),
                        DeliverAt = DateTime.Now.AddHours(2),
                        Intent = MessageIntentEnum.Reply,
                        ReplyToAddress = new Address("Foo2","Machine2"),
                    }, new TransportMessage
                    {
                        Body = new byte[1024*5]
                    },"MyMessage"),
                });

            persister.SetAsDispatched(id);
            persister.SetAsDispatched(id);
        }
    }
}