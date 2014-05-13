namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Transactions;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_sagas_on_transactional_endpoints : InMemoryFixture
    {
        [Test]
        public void Ambient_transaction_should_commit_saga()
        {
            using (var transactionScope = new TransactionScope())
            {

                SagaPersister.Save(new TestSaga
                                   {
                                       Id = Guid.NewGuid()
                                   });

                FlushSession();
                transactionScope.Complete();
            }

            using (var session = SessionFactory.OpenSession())
            {
                Assert.AreEqual(1, session.CreateCriteria(typeof(TestSaga)).List<TestSaga>().Count);
            }
        }

        [Test]
        public void Ambient_transaction_should_rollback_saga()
        {
            using (new TransactionScope())
            {
                SagaPersister.Save(new TestSaga
                {
                    Id = Guid.NewGuid()
                });
            }

            using (var session = SessionFactory.OpenSession())
            {
                Assert.AreEqual(0, session.CreateCriteria(typeof(TestSaga)).List<TestSaga>().Count);
            }
        }
    }
}