namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NUnit.Framework;
    using Timeout.Core;

    [TestFixture]
    class When_removing_timeouts_from_the_storage : InMemoryDBFixture
    {
        [Test]
        public async Task TryRemove_should_remove_timeouts_by_id()
        {
            var t1 = new TimeoutData
            {
                Time = DateTime.Now.AddYears(-1),
                OwningTimeoutManager = "MyTestEndpoint",
                Headers = new Dictionary<string, string>
                                   {
                                       {"Header1", "Value1"}
                                   }
            };
            var t2 = new TimeoutData
            {
                Time = DateTime.Now.AddYears(-1),
                OwningTimeoutManager = "MyTestEndpoint",
                Headers = new Dictionary<string, string>
                                   {
                                       {"Header1", "Value1"}
                                   }
            };

            await persister.Add(t1, new ContextBag()).ConfigureAwait(false);
            await persister.Add(t2, new ContextBag()).ConfigureAwait(false);

            var timeouts = await persister.GetNextChunk(DateTime.UtcNow.AddYears(-3)).ConfigureAwait(false);

            foreach (var timeout in timeouts.DueTimeouts)
            {
                var timeoutRemoved = await persister.TryRemove(timeout.Id, new ContextBag()).ConfigureAwait(false);
                Assert.IsTrue(timeoutRemoved);
            }

            using (var session = sessionFactory.OpenSession())
            {
                Assert.Null(session.Get<TimeoutEntity>(new Guid(t1.Id)));
                Assert.Null(session.Get<TimeoutEntity>(new Guid(t2.Id)));
            }
        }

        [Test]
        public async Task TryRemove_should_return_false_when_timeout_already_deleted()
        {
            var timeout = new TimeoutData();

            await persister.Add(timeout, new ContextBag()).ConfigureAwait(false);

            Assert.IsTrue(await persister.TryRemove(timeout.Id, new ContextBag()).ConfigureAwait(false));
            Assert.IsFalse(await persister.TryRemove(timeout.Id, new ContextBag()).ConfigureAwait(false));
        }

        [Test]
        public async Task Peek_should_return_no_timeout_when_timeout_already_deleted()
        {
            var timeout = new TimeoutData();

            await persister.Add(timeout, new ContextBag()).ConfigureAwait(false);

            Assert.IsTrue(await persister.TryRemove(timeout.Id, new ContextBag()).ConfigureAwait(false));

            var timeoutData = await persister.Peek(timeout.Id, new ContextBag()).ConfigureAwait(false);
            Assert.IsNull(timeoutData);
        }

        [Test]
        public async Task TryRemove_should_work_with_concurrent_transactions()
        {
            var timeout = new TimeoutData
            {
                Time = DateTime.Now
            };

            await persister.Add(timeout, new ContextBag()).ConfigureAwait(false);

            var t1EnteredTx = new AutoResetEvent(false);
            var t2EnteredTx = new AutoResetEvent(false);

            var task1 = Task.Run(() =>
            {
                t1EnteredTx.Set();
                t2EnteredTx.WaitOne();
                return persister.TryRemove(timeout.Id, new ContextBag());
            });

            var task2 = Task.Run(() =>
            {
                t2EnteredTx.Set();
                t1EnteredTx.WaitOne();
                return persister.TryRemove(timeout.Id, new ContextBag());
            });

            var results = await Task.WhenAll(task1, task2).ConfigureAwait(false);
            Assert.IsTrue(results.Single(x => x));
        }

        [Test]
        public async Task RemoveTimeoutBy_should_remove_timeouts_by_sagaid()
        {
            var sagaId1 = Guid.NewGuid();
            var sagaId2 = Guid.NewGuid();
            var t1 = new TimeoutData
            {
                SagaId = sagaId1,
                Time = DateTime.Now.AddYears(1),
                OwningTimeoutManager = "MyTestEndpoint",
                Headers = new Dictionary<string, string>
                                   {
                                       {"Header1", "Value1"}
                                   }
            };
            var t2 = new TimeoutData
            {
                SagaId = sagaId2,
                Time = DateTime.Now.AddYears(1),
                OwningTimeoutManager = "MyTestEndpoint",
                Headers = new Dictionary<string, string>
                                   {
                                       {"Header1", "Value1"}
                                   }
            };

            await persister.Add(t1, new ContextBag()).ConfigureAwait(false);
            await persister.Add(t2, new ContextBag()).ConfigureAwait(false);

            await persister.RemoveTimeoutBy(sagaId1, new ContextBag()).ConfigureAwait(false);
            await persister.RemoveTimeoutBy(sagaId2, new ContextBag()).ConfigureAwait(false);

            using (var session = sessionFactory.OpenSession())
            {
                var timeoutEntity = session.Get<TimeoutEntity>(new Guid(t1.Id));
                Assert.Null(timeoutEntity);
                Assert.Null(session.Get<TimeoutEntity>(new Guid(t2.Id)));
            }
        }
    }
}