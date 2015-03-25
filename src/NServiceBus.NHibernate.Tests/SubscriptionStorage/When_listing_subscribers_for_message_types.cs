namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
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

            var subscriptionsForMessageType = (await storage.GetSubscriberAddressesForMessage(MessageTypes.MessageA, new ContextBag()).ConfigureAwait(false)).ToArray();

            Assert.AreEqual(2,subscriptionsForMessageType.Length);
            Assert.AreEqual(TestClients.ClientA.Endpoint, subscriptionsForMessageType.First().Endpoint);
            Assert.AreEqual(TestClients.ClientA.TransportAddress, subscriptionsForMessageType.First().TransportAddress);
        }

        [Test]
        public async Task Duplicates_should_not_be_generated_for_interface_inheritance_chains()
        {
            await storage.Subscribe(TestClients.ClientA, new[] { new MessageType(typeof(ISomeInterface)) }, new ContextBag()).ConfigureAwait(false);
            await storage.Subscribe(TestClients.ClientA, new[] { new MessageType(typeof(ISomeInterface2)) }, new ContextBag()).ConfigureAwait(false);
            await storage.Subscribe(TestClients.ClientA, new[] { new MessageType(typeof(ISomeInterface3)) }, new ContextBag()).ConfigureAwait(false);

            var subscriptionsForMessageType = await storage.GetSubscriberAddressesForMessage(new[] {  new MessageType(typeof(ISomeInterface)), new MessageType(typeof(ISomeInterface2)), new MessageType(typeof(ISomeInterface3)) }, new ContextBag()).ConfigureAwait(false);

            Assert.AreEqual(1,subscriptionsForMessageType.Count());
        }
    }
}