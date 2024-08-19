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
        public void Should_throw_exception()
        {
            var exception = Assert.ThrowsAsync<Exception>(() =>
            {
                return Scenario.Define<Context>()
                    .WithEndpoint<NonDtcReceivingEndpoint>(b => b.When(session => session.SendLocal(new MyMessage())).DoNotFailOnErrorMessages())
                    .Done(c => c.FailedMessages.Any())
                    .Run(TimeSpan.FromSeconds(20));
            });

            Assert.That(exception.Message.Contains("ReceiveOnly") && exception.Message.Contains("TransportTransactionMode"), Is.True);
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