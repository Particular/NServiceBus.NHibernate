namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Support;
    using Timeout.Core;

    [TestFixture]
    public class When_fetching_timeouts_from_storage : InMemoryDBFixture
    {
        [Test]
        public void GetNextChunk_should_return_the_complete_list_of_timeouts()
        {
            const int numberOfTimeoutsToAdd = 10;

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                persister.Add(new TimeoutData
                                  {
                                      Time = DateTime.UtcNow.AddHours(-1),
                                      CorrelationId = "boo",
                                      Destination = new Address("timeouts", RuntimeEnvironment.MachineName),
                                      SagaId = Guid.NewGuid(),
                                      State = new byte[] { 0, 0, 133 },
                                      Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "aString1" }, { "Super", "aString2" } },
                                      OwningTimeoutManager = Configure.EndpointName,
                                  });
            }
            DateTime nextTimeToRunQuery;
            Assert.AreEqual(numberOfTimeoutsToAdd, persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery).Count());
        }

        [Test]
        public void GetNextChunk_should_return_the_next_time_of_retrieval()
        {
            var nextTime = DateTime.UtcNow.AddHours(1);

            persister.Add(new TimeoutData
            {
                Time = nextTime,
                CorrelationId = "boo",
                Destination = new Address("timeouts", RuntimeEnvironment.MachineName),
                SagaId = Guid.NewGuid(),
                State = new byte[] { 0, 0, 133 },
                Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "aString1" }, { "Super", "aString2" } },
                OwningTimeoutManager = Configure.EndpointName,
            });
            


            DateTime nextTimeToRunQuery;
            persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery);

            Assert.IsTrue((nextTime - nextTimeToRunQuery).TotalSeconds < 1);
        }

        [Test]
        public void Peek_should_return_timeout_with_id()
        {
            var timeoutData = new TimeoutData
            {
                Time = DateTime.UtcNow.AddHours(-1),
                CorrelationId = "boo",
                Destination = new Address("timeouts", RuntimeEnvironment.MachineName),
                SagaId = Guid.NewGuid(),
                State = new byte[] { 0, 0, 133 },
                Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "aString1" }, { "Super", "aString2" } },
                OwningTimeoutManager = Configure.EndpointName,
            };
            persister.Add(timeoutData);

            var result = persister.Peek(timeoutData.Id);

            Assert.AreEqual(timeoutData.Id, result.Id);
            Assert.AreEqual(timeoutData.Time.ToString("G"), result.Time.ToString("G"));
            Assert.AreEqual(timeoutData.CorrelationId, result.CorrelationId);
            Assert.AreEqual(timeoutData.Destination, result.Destination);
            Assert.AreEqual(timeoutData.SagaId, result.SagaId);
            Assert.AreEqual(timeoutData.State, result.State);
            Assert.AreEqual(timeoutData.Headers, result.Headers);
            Assert.AreEqual(timeoutData.OwningTimeoutManager, result.OwningTimeoutManager);
        }

        [Test]
        public void Peek_should_return_null_when_no_existing_timeout_with_id()
        {
            var timeoutData = new TimeoutData
            {
                Time = DateTime.UtcNow.AddHours(-1),
                CorrelationId = "boo",
                Destination = new Address("timeouts", RuntimeEnvironment.MachineName),
                SagaId = Guid.NewGuid(),
                State = new byte[] { 0, 0, 133 },
                Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "aString1" }, { "Super", "aString2" } },
                OwningTimeoutManager = Configure.EndpointName,
            };
            persister.Add(timeoutData);

            var result = persister.Peek(Guid.NewGuid().ToString());

            Assert.IsNull(result);
        }
    }
}