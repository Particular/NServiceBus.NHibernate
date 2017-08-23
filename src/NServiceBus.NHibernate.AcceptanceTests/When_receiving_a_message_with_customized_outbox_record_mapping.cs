namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;
    using NHibernate.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using Settings;
    using NUnit.Framework;

    public class When_receiving_a_message_with_customized_outbox_record_mapping : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_handle_it()
        {
            Requires.OutboxPersistence();

            var result = await Scenario.Define<Context>()
                .WithEndpoint<NonDtcReceivingEndpoint>(b => b.When(session => session.SendLocal(new PlaceOrder())))
                .Done(c => c.OrderAckReceived == 1)
                .Run();

            Assert.AreEqual(1, result.OrderAckReceived);
        }

        class Context : ScenarioContext
        {
            public int OrderAckReceived { get; set; }
        }

        public class NonDtcReceivingEndpoint : EndpointConfigurationBuilder
        {
            public NonDtcReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.EnableOutbox();
                    b.UsePersistence<NHibernatePersistence>().UseOutboxRecord<MessageIdOutboxRecord, MessageIdOutboxRecordMapping>();
                });
            }

            class PlaceOrderHandler : IHandleMessages<PlaceOrder>
            {
                public Task Handle(PlaceOrder message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new SendOrderAcknowledgement
                    {
                        MessageId = context.MessageId
                    });
                }
            }

            class SendOrderAcknowledgementHandler : IHandleMessages<SendOrderAcknowledgement>
            {
                public Context Context { get; set; }

                public ReadOnlySettings Settings { get; set; }

                public Task Handle(SendOrderAcknowledgement message, IMessageHandlerContext context)
                {
                    var session = context.SynchronizedStorageSession.Session();
                    var recordId = Settings.EndpointName() + "/" + message.MessageId;
                    var record = session.Get<MessageIdOutboxRecord>(recordId);
                    if (record != null)
                    {
                        Context.OrderAckReceived++;
                    }
                    return Task.FromResult(0);
                }
            }
        }

        public class PlaceOrder : ICommand
        {
        }

        public class SendOrderAcknowledgement : IMessage
        {
            public string MessageId { get; set; }
        }

        class MessageIdOutboxRecord : IOutboxRecord
        {
            public virtual string MessageId { get; set; }
            public virtual bool Dispatched { get; set; }
            public virtual DateTime? DispatchedAt { get; set; }
            public virtual string TransportOperations { get; set; }
        }

        class MessageIdOutboxRecordMapping : ClassMapping<MessageIdOutboxRecord>
        {
            public MessageIdOutboxRecordMapping()
            {
                Table("MessageIdOutboxRecordMapping");
                EntityName("MessageIdOutboxRecord");
                Id(x => x.MessageId, m => m.Generator(Generators.Assigned));
                Property(p => p.Dispatched, pm =>
                {
                    pm.Column(c => c.NotNullable(true));
                });
                Property(p => p.DispatchedAt);
                Property(p => p.TransportOperations, pm => pm.Type(NHibernateUtil.StringClob));
            }
        }
    }
}