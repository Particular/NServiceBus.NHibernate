namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Timeout.Core;

    [TestFixture]
    class When_fetching_timeouts_from_storage : InMemoryDBFixture
    {
        [Test]
        public async Task GetNextChunk_should_return_the_complete_list_of_timeouts()
        {
            const int numberOfTimeoutsToAdd = 10;

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                await persister.Add(new TimeoutData
                {
                    Time = DateTime.UtcNow.AddHours(-1),
                    Destination = "timeouts",
                    SagaId = Guid.NewGuid(),
                    State = new byte[] { 0, 0, 133 },
                    Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "aString1" }, { "Super", "aString2" } },
                    OwningTimeoutManager = "MyTestEndpoint",
                }, new ContextBag()).ConfigureAwait(false);
            }
            var result = await persister.GetNextChunk(DateTime.UtcNow.AddYears(-3)).ConfigureAwait(false);
            Assert.AreEqual(numberOfTimeoutsToAdd, result.DueTimeouts.Count());
        }

        [Test]
        public async Task GetNextChunk_should_return_the_next_time_of_retrieval()
        {
            var nextTime = DateTime.UtcNow.AddHours(1);

            await persister.Add(new TimeoutData
            {
                Time = nextTime,
                Destination = "timeouts",
                SagaId = Guid.NewGuid(),
                State = new byte[] { 0, 0, 133 },
                Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "aString1" }, { "Super", "aString2" } },
                OwningTimeoutManager = "MyTestEndpoint"
            }, new ContextBag()).ConfigureAwait(false);



            var result = await persister.GetNextChunk(DateTime.UtcNow.AddYears(-3)).ConfigureAwait(false);

            Assert.IsTrue((nextTime - result.NextTimeToQuery).TotalSeconds < 1);
        }

        [Test]
        public async Task Peek_should_return_timeout_with_id()
        {
            var uniqueId = Guid.NewGuid().ToString();
            var timeoutData = new TimeoutData
            {
                Time = DateTime.UtcNow.AddHours(-1),
                Destination = "timeouts",
                SagaId = Guid.NewGuid(),
                State = new byte[] { 0, 0, 133 },
                Headers = new Dictionary<string, string>
                {
                    { "Bar", "34234" },
                    { "Foo", "aString1" },
                    { "Super", "aString2" },
                    { Headers.MessageId, uniqueId}
                },
                OwningTimeoutManager = "MyTestEndpoint"
            };
            await persister.Add(timeoutData, new ContextBag()).ConfigureAwait(false);

            var result = await persister.Peek(timeoutData.Id, new ContextBag()).ConfigureAwait(false);

            Assert.AreEqual(timeoutData.Id, result.Id);
            Assert.AreEqual(timeoutData.Time.ToString("G"), result.Time.ToString("G"));
            Assert.AreEqual(timeoutData.Destination, result.Destination);
            Assert.AreEqual(timeoutData.SagaId, result.SagaId);
            Assert.AreEqual(timeoutData.State, result.State);
            Assert.AreEqual(timeoutData.Headers, result.Headers);
        }

        [Test]
        public async Task Peek_should_return_null_when_no_existing_timeout_with_id()
        {
            var result = await persister.Peek(Guid.NewGuid().ToString(), new ContextBag()).ConfigureAwait(false);
            Assert.IsNull(result);
        }
    }
}