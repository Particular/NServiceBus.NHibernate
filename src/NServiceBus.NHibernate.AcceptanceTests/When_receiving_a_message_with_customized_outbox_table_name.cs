namespace NServiceBus.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Driver;
    using global::NHibernate.SqlCommand;
    using NHibernate.Outbox;
    using NUnit.Framework;
    using Persistence;
    using Environment = global::NHibernate.Cfg.Environment;

    public class When_receiving_a_message_with_customized_outbox_table_name : NServiceBusAcceptanceTest
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
                        persistence.CustomizeOutboxTableName("MyOutbox", "dbo");
                    })
                    .When(session => session.SendLocal(new PlaceOrder())))
                .Done(c => c.Done)
                .Run();

            Assert.IsTrue(result.Done);
            StringAssert.StartsWith("INSERT INTO dbo.MyOutbox", result.OutboxTableInsert);
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
                if (sql.StartsWithCaseInsensitive("insert into "))
                {
                    context.OutboxTableInsert = sql.ToString();
                }
                return sql;
            }
        }

        class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public string OutboxTableInsert { get; set; }
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
                    return context.SendLocal(new SendOrderAcknowledgement());
                }
            }

            class SendOrderAcknowledgementHandler : IHandleMessages<SendOrderAcknowledgement>
            {
                public Context Context { get; set; }

                public Task Handle(SendOrderAcknowledgement message, IMessageHandlerContext context)
                {
                    Context.Done = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class PlaceOrder : ICommand
        {
        }

        public class SendOrderAcknowledgement : IMessage
        {
        }
    }
}