namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_initializing_the_storage_with_existing_v2X_subscriptions : InMemoryDBFixture
    {
        [Test]
        public async Task Should_automatically_update_them_to_the_30_format()
        {
            using (var session = SessionFactory.OpenSession())
            {
                var command = session.Connection.CreateCommand();

                command.CommandText = $"INSERT INTO Subscription([SubscriberEndpoint],[MessageType]) values ('{TestClients.ClientA.TransportAddress}','{typeof(MessageB).AssemblyQualifiedName}')";

                command.ExecuteNonQuery();
            }

            storage.Init();

            var subscriptionsForMessageType = (await storage.GetSubscriberAddressesForMessage(MessageTypes.MessageB, new ContextBag()).ConfigureAwait(false)).ToArray();

            Assert.AreEqual(1, subscriptionsForMessageType.Length);
            Assert.AreEqual(TestClients.ClientA.Endpoint, subscriptionsForMessageType.First().Endpoint);
            Assert.AreEqual(TestClients.ClientA.TransportAddress, subscriptionsForMessageType.First().TransportAddress);
        }
    }
}