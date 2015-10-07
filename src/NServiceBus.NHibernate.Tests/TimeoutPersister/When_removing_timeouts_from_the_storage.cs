namespace NServiceBus.TimeoutPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Transactions;
    using NUnit.Framework;
    using Support;
    using Timeout.Core;

    [TestFixture]
    public class When_removing_timeouts_from_the_storage : InMemoryDBFixture
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
                CorrelationId = "boo",
                Destination = new Address("timeouts", RuntimeEnvironment.MachineName),
                SagaId = Guid.NewGuid(),
                State = new byte[] { 1, 1, 133, 200 },
                Headers = headers,
                OwningTimeoutManager = Configure.EndpointName,
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
                OwningTimeoutManager = Configure.EndpointName,
                Headers = new Dictionary<string, string>
                                   {
                                       {"Header1", "Value1"}
                                   }
            };
            var t2 = new TimeoutData
            {
                Time = DateTime.Now.AddYears(-1),
                OwningTimeoutManager = Configure.EndpointName,
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
            bool? t1Result = null;
            bool? t2Result = null;

            var t1 = new Thread(() =>
            {
                using (var tx = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted
                }))
                {
                    t1EnteredTx.Set();
                    t2EnteredTx.WaitOne();
                    t1Result = persister.TryRemove(timeout.Id);
                    tx.Complete();
                }
            });

            var t2 = new Thread(() =>
            {
                using (var tx = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted
                }))
                {
                    t2EnteredTx.Set();
                    t1EnteredTx.WaitOne();
                    t2Result = persister.TryRemove(timeout.Id);
                    tx.Complete();
                }
            });

            t1.Start();
            t2.Start();
            t1.Join(TimeSpan.FromSeconds(30));
            t2.Join(TimeSpan.FromSeconds(30));

            Assert.IsTrue(t1Result.HasValue && t2Result.HasValue);

            // one delete should succeed, the other one shouldn't
            Assert.IsTrue(t1Result.Value || t2Result.Value);
            Assert.IsFalse(t1Result.Value && t2Result.Value);
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
                OwningTimeoutManager = Configure.EndpointName,
                Headers = new Dictionary<string, string>
                                   {
                                       {"Header1", "Value1"}
                                   }
            };
            var t2 = new TimeoutData
            {
                SagaId = sagaId2,
                Time = DateTime.Now.AddYears(1),
                OwningTimeoutManager = Configure.EndpointName,
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