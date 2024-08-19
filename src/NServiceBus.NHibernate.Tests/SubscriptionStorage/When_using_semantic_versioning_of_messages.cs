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

            Assert.Multiple(async () =>
            {
                Assert.That((await storage.GetSubscriberAddressesForMessage([MessageTypes.MessageA], new ContextBag())).Count(), Is.EqualTo(3));
                Assert.That((await storage.GetSubscriberAddressesForMessage([MessageTypes.MessageAv11], new ContextBag())).Count(), Is.EqualTo(3));
                Assert.That((await storage.GetSubscriberAddressesForMessage([MessageTypes.MessageAv2], new ContextBag())).Count(), Is.EqualTo(3));
            });
        }

        [Test]
        public async Task Unsubscribe_should_work_even_if_subscribed_version_is_different()
        {
            await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageA, new ContextBag());

            Assert.That((await storage.GetSubscriberAddressesForMessage([MessageTypes.MessageA], new ContextBag())).Count(), Is.EqualTo(1));

            await storage.Unsubscribe(TestClients.ClientA, MessageTypes.MessageAv2, new ContextBag());

            Assert.That((await storage.GetSubscriberAddressesForMessage([MessageTypes.MessageA], new ContextBag())).Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task Subsribe_should_override_version()
        {
            await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageA, new ContextBag());

            Assert.That((await storage.GetSubscriberAddressesForMessage([MessageTypes.MessageA], new ContextBag())).Count(), Is.EqualTo(1));

            await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageAv2, new ContextBag());

            Assert.That((await storage.GetSubscriberAddressesForMessage([MessageTypes.MessageA], new ContextBag())).Count(), Is.EqualTo(1));
        }
    }
}