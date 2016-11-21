namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_receiving_a_unsubscribe_message : InMemoryDBFixture
    {
        [Test]
        public async Task All_subscription_entries_for_specified_message_types_should_be_removed()
        {
            await storage.Subscribe(TestClients.ClientA, new MessageType(typeof(MessageA)), new ContextBag()).ConfigureAwait(false);
            await storage.Subscribe(TestClients.ClientA, new MessageType(typeof(MessageB)), new ContextBag()).ConfigureAwait(false);

            await storage.Unsubscribe(TestClients.ClientA, new MessageType(typeof(MessageA)), new ContextBag()).ConfigureAwait(false);
            await storage.Unsubscribe(TestClients.ClientA, new MessageType(typeof(MessageB)), new ContextBag()).ConfigureAwait(false);

            using (var session = SessionFactory.OpenSession())
            {
                var subscriptions = session.CreateCriteria(typeof(Subscription)).List<Subscription>();
                Assert.AreEqual(subscriptions.Count, 0);
            }
        }
    }
}