namespace NServiceBus.AcceptanceTests.Outbox
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_a_duplicate_message_arrives_in_pessimistic_mode : NServiceBusAcceptanceTest
    {
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
                }))
                .Done(c => c.MessagesReceived >= 2)
                .Run();

            Assert.AreEqual(1, context.HandlerExecutions);
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
                    var outboxSettings = b.EnableOutbox();
                    outboxSettings.UsePessimisticConcurrencyControl();

                    b.Pipeline.Register(typeof(TerminatorBehavior), "Terminator");
                    var recoverability = b.Recoverability();
                    recoverability.Immediate(immediate => immediate.NumberOfRetries(5));
                });
            }

            class PlaceOrderHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public async Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    await Task.Delay(2000);
                    Context.IncrementHandlerExecutions();
                }
            }
        }

        public class TerminatorBehavior : Behavior<ITransportReceiveContext>
        {
            public Context Context { get; set; }

            public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
            {
                await next().ConfigureAwait(false);
                Context.IncrementMessagesReceived();
            }
        }

        public class MyMessage : ICommand
        {
        }
    }
}