namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_using_outbox_with_transport_in_transaction_scope_mode : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_throw_exception()
        {
            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<NonDtcReceivingEndpoint>(b => b.When(session => session.SendLocal(new MyMessage())).DoNotFailOnErrorMessages())
                .Done(c => c.FailedMessages.Any())
                .Run(TimeSpan.FromSeconds(20));

            Assert.IsTrue(ctx.FailedMessages.Values.SelectMany(x => x).Any(m => m.Exception.Message.StartsWith("The endpoint is configured to use Outbox but a TransactionScope has been detected.")));
        }

        class Context : ScenarioContext
        {
        }

        public class NonDtcReceivingEndpoint : EndpointConfigurationBuilder
        {
            public NonDtcReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>((b, r) =>
                {
                    b.EnableOutbox();
                    b.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.TransactionScope;
                });
            }

            class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}