namespace NServiceBus.AcceptanceTests
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Features;
    using NServiceBus.AcceptanceTests.PubSub;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class Issue_67 : NServiceBusAcceptanceTest
    {
        static TimeSpan SlrDelay = TimeSpan.FromSeconds(1);

        [Test]
        public void Subscribers_retry_with_SLR()
        {
            var ctx = new Context();

            Scenario.Define(ctx)
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscriber1Subscribed && c.Subscriber2Subscribed, bus => bus.Publish(new MyEvent()))
                )
                .WithEndpoint<Subscriber1>(b => b.Given((bus, context) =>
                {
                    bus.Subscribe<MyEvent>();

                    if (context.HasNativePubSubSupport)
                    {
                        context.Subscriber1Subscribed = true;
                    }
                }))
                .WithEndpoint<Subscriber2>(b => b.Given((bus, context) =>
                {
                    bus.Subscribe<MyEvent>();

                    if (context.HasNativePubSubSupport)
                    {
                        context.Subscriber2Subscribed = true;
                    }
                }))
                .AllowExceptions(e => e.Message.Contains("Simulated exception"))
                .Done(c => c.NumberOfSlrRetriesPerformedForSubscriber1 == 3 && c.NumberOfSlrRetriesPerformedForSubscriber2 == 3)
                .Run();

            Assert.AreEqual(3, ctx.NumberOfSlrRetriesPerformedForSubscriber1);
            Assert.AreEqual(3, ctx.NumberOfSlrRetriesPerformedForSubscriber2);
        }

        public class Context : ScenarioContext
        {
            public bool Subscriber1Subscribed { get; set; }
            public bool Subscriber2Subscribed { get; set; }
            public int NumberOfSlrRetriesPerformedForSubscriber1 { get; set; }
            public int NumberOfSlrRetriesPerformedForSubscriber2 { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.OnEndpointSubscribed<Context>((s, context) =>
                    {
                        if (s.SubscriberReturnAddress.Queue.Contains("Subscriber1"))
                        {
                            context.Subscriber1Subscribed = true;
                        }


                        if (s.SubscriberReturnAddress.Queue.Contains("Subscriber2"))
                        {
                            context.Subscriber2Subscribed = true;
                        }
                    });
                });
            }
        }

        public class Subscriber1 : EndpointConfigurationBuilder
        {
            public Subscriber1()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0; //to skip the FLR
                    })
                    .WithConfig<SecondLevelRetriesConfig>(c =>
                    {
                        c.NumberOfRetries = 3;
                        c.TimeIncrease = SlrDelay;
                    })
                    .AddMapping<MyEvent>(typeof(Publisher));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public void Handle(MyEvent messageThatIsEnlisted)
                {
                    if (Bus.CurrentMessageContext.Headers.ContainsKey(Headers.Retries))
                    {
                        var numberOfSlrRetriesPerformed = Int32.Parse(Bus.CurrentMessageContext.Headers[Headers.Retries]);
                        Context.NumberOfSlrRetriesPerformedForSubscriber1 = numberOfSlrRetriesPerformed;
                    }

                    throw new Exception("Simulated exception");
                }
            }
        }

        public class Subscriber2 : EndpointConfigurationBuilder
        {
            public Subscriber2()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0; //to skip the FLR
                    })
                    .WithConfig<SecondLevelRetriesConfig>(c =>
                    {
                        c.NumberOfRetries = 3;
                        c.TimeIncrease = SlrDelay;
                    })
                    .AddMapping<MyEvent>(typeof(Publisher));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public void Handle(MyEvent messageThatIsEnlisted)
                {
                    if (Bus.CurrentMessageContext.Headers.ContainsKey(Headers.Retries))
                    {
                        var numberOfSlrRetriesPerformed = Int32.Parse(Bus.CurrentMessageContext.Headers[Headers.Retries]);
                        Context.NumberOfSlrRetriesPerformedForSubscriber2 = numberOfSlrRetriesPerformed;
                    }

                    throw new Exception("Simulated exception");
                }
            }
        }

        [Serializable]
        public class MyEvent : IEvent
        {
        }
    }
}