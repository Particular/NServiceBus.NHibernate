using NHibernate.Dialect;

namespace NServiceBus.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using ApprovalTests.Reporters;
    using NServiceBus;
    using NHibernate;
    using NUnit.Framework;

    [TestFixture]
    [UseReporter(typeof(DiffReporter),typeof(AllFailingTestsClipboardReporter))]
    public class DDL
    {
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Outbox()
        {
            var script = ScriptGenerator<MsSql2012Dialect>.GenerateOutboxScript();
            TestApprover.Verify(script);
        }

        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Subscriptions()
        {
            var script = ScriptGenerator<MsSql2012Dialect>.GenerateSubscriptionStoreScript();
            TestApprover.Verify(script);
        }

        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Timeouts()
        {
            var script = ScriptGenerator<MsSql2012Dialect>.GenerateTimeoutStoreScript();
            TestApprover.Verify(script);
        }

        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GatewayDeduplication()
        {
            var script = ScriptGenerator<MsSql2012Dialect>.GenerateGatewayDeduplicationStoreScript();
            TestApprover.Verify(script);
        }

        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void MySaga()
        {
            var script = ScriptGenerator<MsSql2012Dialect>.GenerateSagaScript<MySaga>();
            TestApprover.Verify(script);
        }
    }

    class MySaga : Saga<MySaga.SagaData>, IAmStartedByMessages<MyMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MyMessage>(m => m.UniqueId).ToSaga(s => s.UniqueId);
        }

        public class SagaData : ContainSagaData
        {
            public virtual string UniqueId { get; set; }
            public virtual IList<CollectionEntry> Entries { get; set; }
        }

        public class CollectionEntry
        {
            public virtual Guid Id { get; set; }
            public virtual decimal Value { get; set; }
        }

        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }

    class MyMessage : IMessage
    {
        public string UniqueId { get; set; }
    }
}
