namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Driver;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;
    using global::NHibernate.SqlCommand;
    using NHibernate.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using Settings;
    using NUnit.Framework;
    using Persistence;
    using Environment = global::NHibernate.Cfg.Environment;

    public class When_receiving_a_message_with_customized_outbox_record_mapping : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_handle_it()
        {
            Requires.OutboxPersistence();

            var result = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b
                    .CustomConfig((configuration, context) =>
                    {
                        var cfg = new Configuration();
                        cfg.SetProperty(Environment.Dialect, typeof(MsSql2012Dialect).FullName);
                        cfg.SetProperty(Environment.ConnectionDriver, typeof(Sql2008ClientDriver).FullName);
                        cfg.SetProperty(Environment.ConnectionString, EndpointConfigurer.ConnectionString);
                        cfg.SetInterceptor(new LoggingInterceptor(context));

                        var persistence = configuration.UsePersistence<NHibernatePersistence>();
                        persistence.UseConfiguration(cfg);
                        persistence.UseOutboxRecord<MessageIdOutboxRecord, MessageIdOutboxRecordMapping>();
                    })
                    .When(session => session.SendLocal(new PlaceOrder())))
                .Done(c => c.OrderAckReceived == 1)
                .Run();

            Assert.AreEqual(1, result.OrderAckReceived);
            Assert.IsTrue(result.CorrectTableNameDetected);
        }

        class LoggingInterceptor : EmptyInterceptor
        {
            Context context;

            public LoggingInterceptor(Context context)
            {
                this.context = context;
            }

            public override SqlString OnPrepareStatement(SqlString sql)
            {
                if (sql.StartsWithCaseInsensitive("insert into MessageIdOutboxRecordMapping"))
                {
                    context.CorrectTableNameDetected = true;
                }
                return sql;
            }
        }

        class Context : ScenarioContext
        {
            public int OrderAckReceived { get; set; }
            public bool CorrectTableNameDetected { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.EnableOutbox();
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
                Context testContext;
                ReadOnlySettings settings;

                public SendOrderAcknowledgementHandler(Context testContext, ReadOnlySettings settings)
                {
                    this.testContext = testContext;
                    this.settings = settings;
                }

                public Task Handle(SendOrderAcknowledgement message, IMessageHandlerContext context)
                {
                    var session = context.SynchronizedStorageSession.Session();
                    var recordId = settings.EndpointName() + "/" + message.MessageId;
                    var record = session.Get<MessageIdOutboxRecord>(recordId);
                    if (record != null)
                    {
                        testContext.OrderAckReceived++;
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