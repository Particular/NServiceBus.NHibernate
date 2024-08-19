namespace NServiceBus.AcceptanceTests.Outbox
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    [TestFixture(false)]
    [TestFixture(true)]
    public class When_a_duplicate_message_arrives_in_pessimistic_mode : NServiceBusAcceptanceTest
    {
        bool transactionScope;

        public When_a_duplicate_message_arrives_in_pessimistic_mode(bool transactionScope)
        {
            this.transactionScope = transactionScope;
        }

        [Test]
        public async Task Should_not_invoke_handler_for_a_duplicate_message()
        {
            Requires.OutboxPersistence();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<OutboxEndpoint>(b => b.When(async session =>
                {
                    var duplicateMessageId = Guid.NewGuid().ToString();
                    await Send(duplicateMessageId, session);
                    await Send(duplicateMessageId, session);
                }).CustomConfig(c =>
                {
                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                    var outboxSettings = c.EnableOutbox();
                    outboxSettings.UsePessimisticConcurrencyControl();
                    if (transactionScope)
                    {
                        outboxSettings.UseTransactionScope();
                    }
                }))
                .Done(c => c.MessagesReceived >= 2)
                .Run();

            Assert.That(context.HandlerExecutions, Is.EqualTo(1));
        }

        static Task Send(string duplicateMessageId, IMessageSession session)
        {
            var options = new SendOptions();
            options.SetMessageId(duplicateMessageId);
            options.RouteToThisEndpoint();
            return session.Send(new MyMessage(), options);
        }

        public class Context : ScenarioContext
        {
            int handlerExecutions;
            int messagesReceived;

            public int MessagesReceived => messagesReceived;

            public int HandlerExecutions => handlerExecutions;

            public void IncrementHandlerExecutions()
            {
                Interlocked.Increment(ref handlerExecutions);
            }

            public void IncrementMessagesReceived()
            {
                Interlocked.Increment(ref messagesReceived);
            }
        }


        public class OutboxEndpoint : EndpointConfigurationBuilder
        {
            public OutboxEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.Pipeline.Register(typeof(TerminatorBehavior), "Terminator");
                    var recoverability = b.Recoverability();
                    recoverability.Immediate(immediate => immediate.NumberOfRetries(5));
                });
            }

            class PlaceOrderHandler : IHandleMessages<MyMessage>
            {
                Context testContext;

                public PlaceOrderHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public async Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    await Task.Delay(2000);
                    testContext.IncrementHandlerExecutions();
                }
            }
        }

        public class TerminatorBehavior : Behavior<ITransportReceiveContext>
        {
            Context testContext;

            public TerminatorBehavior(Context testContext)
            {
                this.testContext = testContext;
            }

            public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
            {
                await next().ConfigureAwait(false);
                testContext.IncrementMessagesReceived();
            }
        }

        public class MyMessage : ICommand
        {
        }
    }
}