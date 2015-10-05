namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;
    using Support;
    using Timeout.Core;

    [TestFixture]
    class When_removing_timeouts_from_the_storage : InMemoryDBFixture
    {
        [Test]
        public void TryRemove_should_return_the_correct_headers()
        {
            var headers = new Dictionary<string, string>
                          {
                              {"Bar", "34234"},
                              {"Foo", "aString1"},
                              {"Super", "aString2"}
                          };

            var timeout = new TimeoutData
            {
                Time = DateTime.UtcNow.AddHours(-1),
                Destination = new Address("timeouts", RuntimeEnvironment.MachineName),
                SagaId = Guid.NewGuid(),
                State = new byte[] { 1, 1, 133, 200 },
                Headers = headers,
                OwningTimeoutManager = "MyTestEndpoint",
            };
            persister.Add(timeout);

            TimeoutData timeoutData;
            persister.TryRemove(timeout.Id, out timeoutData);

            CollectionAssert.AreEqual(headers, timeoutData.Headers);
        }

        [Test]
        public void TryRemove_should_remove_timeouts_by_id()
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

            persister.Add(t1);
            persister.Add(t2);

            DateTime nextTimeToRunQuery;
            var timeouts = persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery);

            foreach (var timeout in timeouts)
            {
                TimeoutData timeoutData;
                persister.TryRemove(timeout.Item1, out timeoutData);
            }

            using (var session = sessionFactory.OpenSession())
            {
                Assert.Null(session.Get<TimeoutEntity>(new Guid(t1.Id)));
                Assert.Null(session.Get<TimeoutEntity>(new Guid(t2.Id)));
            }
        }

        [Test]
        public void TryRemove_should_return_false_when_timeout_already_deleted()
        {
            var timeout = new TimeoutData();

            persister.Add(timeout);

            Assert.IsTrue(persister.TryRemove(timeout.Id));
            Assert.IsFalse(persister.TryRemove(timeout.Id));
        }

        [Test]
        public void TryRemove_should_work_with_concurrent_transactions()
        {
            var timeout = new TimeoutData
            {
                Time = DateTime.Now
            };

            persister.Add(timeout);

            var t1EnteredTx = new AutoResetEvent(false);
            var t2EnteredTx = new AutoResetEvent(false);

            var task1 = Task.Factory.StartNew(() =>
            {
                using (var tx = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted
                }))
                {
                    t1EnteredTx.Set();
                    t2EnteredTx.WaitOne();
                    var result = persister.TryRemove(timeout.Id);
                    tx.Complete();
                    return result;
                }
            });

            var task2 = Task.Factory.StartNew(() =>
            {
                using (var tx = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted
                }))
                {
                    t2EnteredTx.Set();
                    t1EnteredTx.WaitOne();
                    var result = persister.TryRemove(timeout.Id);
                    tx.Complete();
                    return result;
                }
            });

            Assert.True(task1.Wait(TimeSpan.FromSeconds(30)));
            Assert.True(task2.Wait(TimeSpan.FromSeconds(30)));

            // one delete should succeed, the other one shouldn't
            Assert.IsTrue(task1.Result || task2.Result);
            Assert.IsFalse(task1.Result && task2.Result);
        }

        [Test]
        public void RemoveTimeoutBy_should_remove_timeouts_by_sagaid()
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

            persister.Add(t1);
            persister.Add(t2);

            persister.RemoveTimeoutBy(sagaId1);
            persister.RemoveTimeoutBy(sagaId2);

            using (var session = sessionFactory.OpenSession())
            {
                Assert.Null(session.Get<TimeoutEntity>(new Guid(t1.Id)));
                Assert.Null(session.Get<TimeoutEntity>(new Guid(t2.Id)));
            }
        }
    }
}