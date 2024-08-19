namespace NServiceBus.NHibernate.Tests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Sagas;
    using Testing;
    using NUnit.Framework;

    [TestFixture]
    class When_automapping_sagas_with_different_correlation_properties : InMemoryFixture
    {
        protected override Type[] SagaTypes => new[]
        {
            typeof(FirstAutomappedSaga),
            typeof(SecondAutomappedSaga)
        };

        [Test]
        public async Task Value_uniqueness_should_be_enforced()
        {
            var firstFailed = false;
            using (var session = SessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                var storageSession = new TestingNHibernateSynchronizedStorageSession(session);

                var correlationProperty = new SagaCorrelationProperty("FirstId", "whatever");

                await SagaPersister.Save(new FirstAutomappedSagaData
                {
                    Id = Guid.NewGuid(),
                    FirstId = "whatever"
                }, correlationProperty, storageSession, new ContextBag()).ConfigureAwait(false);

                await SagaPersister.Save(new FirstAutomappedSagaData
                {
                    Id = Guid.NewGuid(),
                    FirstId = "whatever"
                }, correlationProperty, storageSession, new ContextBag()).ConfigureAwait(false);

                try
                {
                    await transaction.CommitAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    firstFailed = true;
                }
            }

            var secondFailed = false;
            using (var session = SessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                var storageSession = new TestingNHibernateSynchronizedStorageSession(session);

                var correlationProperty = new SagaCorrelationProperty("SecondId", "whatever");

                await SagaPersister.Save(new SecondAutomappedSagaData
                {
                    Id = Guid.NewGuid(),
                    SecondId = "whatever"
                }, correlationProperty, storageSession, new ContextBag()).ConfigureAwait(false);

                await SagaPersister.Save(new SecondAutomappedSagaData
                {
                    Id = Guid.NewGuid(),
                    SecondId = "whatever"
                }, correlationProperty, storageSession, new ContextBag()).ConfigureAwait(false);

                try
                {
                    await transaction.CommitAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    secondFailed = true;
                }
            }

            Assert.That(firstFailed, Is.True);
            Assert.That(secondFailed, Is.True);
        }
    }

    class FirstAutomappedSaga : Saga<FirstAutomappedSagaData>, IAmStartedByMessages<SomeMessage>
    {
        public Task Handle(SomeMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<FirstAutomappedSagaData> mapper)
        {
            mapper.ConfigureMapping<SomeMessage>(m => m.Id).ToSaga(s => s.FirstId);
        }
    }

    public class FirstAutomappedSagaData : ContainSagaData
    {
        public virtual string FirstId { get; set; }
    }

    class SecondAutomappedSaga : Saga<SecondAutomappedSagaData>, IAmStartedByMessages<SomeMessage>
    {
        public Task Handle(SomeMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SecondAutomappedSagaData> mapper)
        {
            mapper.ConfigureMapping<SomeMessage>(m => m.Id).ToSaga(s => s.SecondId);
        }
    }

    public class SecondAutomappedSagaData : ContainSagaData
    {
        public virtual string SecondId { get; set; }
    }

    public class SomeMessage : ICommand
    {
        public string Id { get; set; }
    }
}