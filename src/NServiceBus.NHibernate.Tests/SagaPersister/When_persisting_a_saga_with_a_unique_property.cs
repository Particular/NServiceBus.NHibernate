namespace NServiceBus.SagaPersisters.NHibernate.Tests;

using System;
using System.Threading.Tasks;
using Extensibility;
using NServiceBus.NHibernate.Tests;
using Sagas;
using Testing;
using NUnit.Framework;

[TestFixture]
class When_persisting_a_saga_with_a_unique_property : InMemoryFixture
{
    protected override Type[] SagaTypes => [typeof(SomeSaga)];

    [Test]
    public async Task The_database_should_enforce_the_uniqueness()
    {
        var failed = false;
        using (var session = SessionFactory.OpenSession())
        using (var transaction = session.BeginTransaction())
        {
            var correlationProperty = new SagaCorrelationProperty("UniqueString", "whatever");
            var storageSession = new TestingNHibernateSynchronizedStorageSession(session);

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
                await transaction.CommitAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                failed = true;
            }
        }

        Assert.That(failed, Is.True);
    }
}

class Message
{
    public string UniqueString { get; set; }
}

class SomeSaga : Saga<SagaWithUniqueProperty>, IAmStartedByMessages<Message>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithUniqueProperty> mapper) => mapper.MapSaga(s => s.UniqueString).ToMessage<Message>(m => m.UniqueString);

    public Task Handle(Message message, IMessageHandlerContext context) => throw new NotImplementedException();
}

[LockMode(LockModes.None)]
public class SagaWithUniqueProperty : ContainSagaData
{
    public virtual string UniqueString { get; set; }
}