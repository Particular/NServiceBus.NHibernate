namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Timeout.Core;

    [TestFixture]
    public class When_storing_timeouts : InMemoryDBFixture
    {
        [Test]
        public void Should_use_the_message_id_to_deduplicate()
        {
            var messageId = Guid.NewGuid();

            persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddHours(-1),
                OwningTimeoutManager = Configure.EndpointName,
                Headers = new Dictionary<string, string> { { Headers.MessageId, messageId.ToString() } }
            });

            persister.Add(new TimeoutData
            {
                Time = DateTime.UtcNow.AddHours(-1),
                OwningTimeoutManager = Configure.EndpointName,
                Headers = new Dictionary<string, string> { { Headers.MessageId, messageId.ToString() } }
            });

            DateTime nextTimeToRunQuery;
            Assert.AreEqual(1, persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery).Count());
        }
    }
}