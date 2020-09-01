namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_deferring_a_message : NServiceBusAcceptanceTest
    {
        /*
         * This test was copy/pasted into the local project to tweak the
         * delay value from the original 2 seconds to the current 5 seconds
         * ON the Linux build agent it happens. When using TimeoutManager,
         * which is the case in this test, there is a path in Core that skips
         * the timout storage is the delay is shorter than 2 seconds.
         * https://github.com/Particular/NServiceBus/blob/ead779f33e8bdec6844ee99a892729cfd7b7f0bc/src/NServiceBus.Core/DelayedDelivery/TimeoutManager/StoreTimeoutBehavior.cs#L61-L67
         *
         * On Linux it happens that the timeout dispatch takes a bit and thus
         * the timeout is immediately dispatched to the timeouts satellite causing
         * the timeout to expire a little faster than 2 seconds, e.g. 1.8 seconds
         */
        [Test]
        public async Task Should_delay_delivery()
        {
            var delay = TimeSpan.FromSeconds(5);

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) =>
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(delay);
                    options.RouteToThisEndpoint();

                    c.SentAt = DateTime.UtcNow;

                    return session.Send(new MyMessage(), options);
                }))
                .Done(c => c.WasCalled)
                .Run();

            Assert.GreaterOrEqual(context.ReceivedAt - context.SentAt, delay);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public DateTime SentAt { get; set; }
            public DateTime ReceivedAt { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public MyMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.ReceivedAt = DateTime.UtcNow;
                    testContext.WasCalled = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}