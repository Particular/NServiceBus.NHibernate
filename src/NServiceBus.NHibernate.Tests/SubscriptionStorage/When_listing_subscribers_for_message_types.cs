namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_listing_subscribers_for_message_types : InMemoryDBFixture
    {
        [Test]
        public async Task The_names_of_all_subscribers_should_be_returned()
        {
            await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageA, new ContextBag()).ConfigureAwait(false);
            await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageB, new ContextBag()).ConfigureAwait(false);
            await storage.Subscribe(TestClients.ClientB, MessageTypes.MessageA, new ContextBag()).ConfigureAwait(false);
            await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageAv2, new ContextBag()).ConfigureAwait(false);

            var messageTypes = new List<MessageType>
            {
                MessageTypes.MessageA
            };
            var subscribers = (await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag()).ConfigureAwait(false)).ToArray();

            Assert.AreEqual(2,subscribers.Length);
            Assert.AreEqual(TestClients.ClientA.Endpoint, subscribers.First().Endpoint);
            Assert.AreEqual(TestClients.ClientA.TransportAddress, subscribers.First().TransportAddress);
        }

        [Test]
        public async Task Duplicates_should_not_be_generated_for_interface_inheritance_chains()
        {
            await storage.Subscribe(TestClients.ClientA, new MessageType(typeof(ISomeInterface)), new ContextBag()).ConfigureAwait(false);
            await storage.Subscribe(TestClients.ClientA, new MessageType(typeof(ISomeInterface2)), new ContextBag()).ConfigureAwait(false);
            await storage.Subscribe(TestClients.ClientA, new MessageType(typeof(ISomeInterface3)), new ContextBag()).ConfigureAwait(false);

            var messageTypes = new[]
            {
                new MessageType(typeof(ISomeInterface)),
                new MessageType(typeof(ISomeInterface2)),
                new MessageType(typeof(ISomeInterface3))
            };
            var subscribers = await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag()).ConfigureAwait(false);

            Assert.AreEqual(1,subscribers.Count());
        }
    }
}