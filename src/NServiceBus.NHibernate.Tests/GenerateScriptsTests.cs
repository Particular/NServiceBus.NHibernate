namespace NServiceBus.NHibernate.Tests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using global::NHibernate.Dialect;
using NServiceBus;
using NServiceBus.NHibernate;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class GenerateScriptsTests
{
    [Test]
    public void Outbox_MsSql2012()
    {
        var script = ScriptGenerator<MsSql2012Dialect>.GenerateOutboxScript();
        Approver.Verify(script);
    }

    [Test]
    public void Outbox_Oracle10g()
    {
        var script = ScriptGenerator<Oracle10gDialect>.GenerateOutboxScript();
        Approver.Verify(script);
    }

    [Test]
    public void Subscriptions_MsSql2012()
    {
        var script = ScriptGenerator<MsSql2012Dialect>.GenerateSubscriptionStoreScript();
        Approver.Verify(script);
    }

    [Test]
    public void Subscriptions_Oracle10g()
    {
        var script = ScriptGenerator<Oracle10gDialect>.GenerateSubscriptionStoreScript();
        Approver.Verify(script);
    }

    [Test]
    public void Sagas_MsSql2012()
    {
        var script = ScriptGenerator<MsSql2012Dialect>.GenerateSagaScript<MySaga>();
        Approver.Verify(script);
    }

    [Test]
    public void Sagas_Oracle10g()
    {
        var script = ScriptGenerator<Oracle10gDialect>.GenerateSagaScript<MySaga>();
        Approver.Verify(script);
    }
}

class MySaga : Saga<MySaga.SagaData>, IAmStartedByMessages<MyMessage>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.UniqueId).ToMessage<MyMessage>(m => m.UniqueId);

    public class SagaData : ContainSagaData
    {
        public virtual string UniqueId { get; set; }
        public virtual IList<CollectionEntry> Entries { get; set; }
        public virtual IList<CollectionEntryWithoutId> EntriesWithoutId { get; set; }
    }

    public class CollectionEntry
    {
        public virtual Guid Id { get; set; }
        public virtual decimal Value { get; set; }
    }

    public class CollectionEntryWithoutId
    {
        public virtual decimal Value1 { get; set; }
        public virtual decimal Value2 { get; set; }
    }

    public Task Handle(MyMessage message, IMessageHandlerContext context) => throw new NotImplementedException();
}

class MyMessage : IMessage
{
    public string UniqueId { get; set; }
}