namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.Persistence.NHibernate;
    using NUnit.Framework;

    [TestFixture]
    class When_getting_outbox_messages : InMemoryDBFixture
    {
        [Test]
        public async Task Should_not_include_dispatched_transport_operations()
        {
            var messageId = Guid.NewGuid().ToString("N");
            using (var session = SessionFactory.OpenSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    var transportOperations = new List<OutboxOperation>
                    {
                        new OutboxOperation
                        {
                            MessageId = "1",
                            Headers = new Dictionary<string, string>(),
                            Options = new Dictionary<string, string>(),
                            Message = new byte[0]
                        },
                         new OutboxOperation
                        {
                            MessageId = "2",
                            Headers = new Dictionary<string, string>(),
                            Options = new Dictionary<string, string>(),
                            Message = new byte[0]
                        },
                    };
                    var outboxRecord = new OutboxRecord
                    {
                        Dispatched = true,
                        DispatchedAt = DateTime.UtcNow,
                        MessageId = messageId,
                        TransportOperations = ObjectSerializer.Serialize(transportOperations)
                    };
                    session.Save(outboxRecord);
                    transaction.Commit();
                }
            }

            var outboxMessage = await persister.Get(messageId, new ContextBag());

            Assert.AreEqual(0, outboxMessage.TransportOperations.Count);
        }
    }
}