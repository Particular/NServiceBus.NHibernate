namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Extensibility;
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

        await storage.Init();

        var messageTypes = new List<MessageType> { MessageTypes.MessageB };
        var subscribers = (await storage.GetSubscriberAddressesForMessage(messageTypes, new ContextBag()).ConfigureAwait(false)).ToArray();

        Assert.That(subscribers.Length, Is.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(subscribers.First().Endpoint, Is.EqualTo(TestClients.ClientA.Endpoint));
            Assert.That(subscribers.First().TransportAddress, Is.EqualTo(TestClients.ClientA.TransportAddress));
        });
    }
}