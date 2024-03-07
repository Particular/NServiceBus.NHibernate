namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_using_semantic_versioning_of_messages : InMemoryDBFixture
    {
        [Test]
        public async Task Only_changes_in_major_version_should_effect_subscribers()
        {
            await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageA, new ContextBag());
            await storage.Subscribe(TestClients.ClientB, MessageTypes.MessageAv11, new ContextBag());
            await storage.Subscribe(TestClients.ClientC, MessageTypes.MessageAv2, new ContextBag());

            Assert.AreEqual(3, (await storage.GetSubscriberAddressesForMessage([MessageTypes.MessageA], new ContextBag())).Count());
            Assert.AreEqual(3, (await storage.GetSubscriberAddressesForMessage([MessageTypes.MessageAv11], new ContextBag())).Count());
            Assert.AreEqual(3, (await storage.GetSubscriberAddressesForMessage([MessageTypes.MessageAv2], new ContextBag())).Count());
        }

        [Test]
        public async Task Unsubscribe_should_work_even_if_subscribed_version_is_different()
        {
            await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageA, new ContextBag());

            Assert.AreEqual(1, (await storage.GetSubscriberAddressesForMessage([MessageTypes.MessageA], new ContextBag())).Count());

            await storage.Unsubscribe(TestClients.ClientA, MessageTypes.MessageAv2, new ContextBag());

            Assert.AreEqual(0, (await storage.GetSubscriberAddressesForMessage([MessageTypes.MessageA], new ContextBag())).Count());
        }

        [Test]
        public async Task Subsribe_should_override_version()
        {
            await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageA, new ContextBag());

            Assert.AreEqual(1, (await storage.GetSubscriberAddressesForMessage([MessageTypes.MessageA], new ContextBag())).Count());

            await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageAv2, new ContextBag());

            Assert.AreEqual(1, (await storage.GetSubscriberAddressesForMessage([MessageTypes.MessageA], new ContextBag())).Count());
        }
    }
}