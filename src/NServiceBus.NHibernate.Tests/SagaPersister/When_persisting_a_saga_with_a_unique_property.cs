namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.NHibernate;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_a_saga_with_a_unique_property : InMemoryFixture<SomeSaga>
    {
        [Test]
        public async Task The_database_should_enforce_the_uniqueness()
        {
            var failed = false;
            using (var session = SessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                var correlationProperty = new SagaCorrelationProperty("UniqueString", "whatever");
                var storageSession = new NHibernateNativeTransactionSynchronizedStorageSession(session, transaction, false);

                await SagaPersister.Save(new SagaWithUniqueProperty
                {
                    Id = Guid.NewGuid(),
                    UniqueString = "whatever"
                }, correlationProperty, storageSession, new ContextBag()).ConfigureAwait(false);

                await SagaPersister.Save(new SagaWithUniqueProperty
                {
                    Id = Guid.NewGuid(),
                    UniqueString = "whatever"
                }, correlationProperty, storageSession, new ContextBag()).ConfigureAwait(false);

                try
                {
                    transaction.Commit();
                }
                catch (Exception)
                {
                    failed = true;
                }
            }
            Assert.IsTrue(failed);
        }
    }

    class SomeSaga : Saga<SagaWithUniqueProperty>, IAmStartedByMessages<IMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithUniqueProperty> mapper)
        {
            mapper.ConfigureMapping<Message>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }

        class Message
        {
            public string UniqueString { get; set; }
        }

        public Task Handle(IMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }

    [LockMode(LockModes.None)]
    public class SagaWithUniqueProperty : IContainSagaData
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual string UniqueString { get; set; }
    }


}