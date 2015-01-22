namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NServiceBus.Saga;
    using NUnit.Framework;

    public class When_trying_to_integrate_with_sql_transport_with_transaction_scope_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public void It_throws_descriptive_exception()
        {
            Environment.SetEnvironmentVariable("Transport.UseSpecific", "SqlServerTransport");
            var context = new Context
            {
                RunId = Guid.NewGuid()
            };

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>()
                    .AllowExceptions()
                    .Done(c => c.Done)
                    .Run();

            Assert.IsNotNull(context.Error);
            Assert.IsTrue(context.Error.Message.StartsWith("In order for NHibernate persistence to work with SQLServer transport"));
        }

        public class Context : ScenarioContext
        {
            public Guid RunId { get; set; }
            public bool Done { get; set; }
            public Exception Error { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(bc =>
                {
                    bc.Transactions().DisableDistributedTransactions();
                    bc.DisableFeature<SecondLevelRetries>();
                }).WithConfig<TransportConfig>(c =>
                {
                    c.MaxRetries = 0;
                });
            }

            //Force enabling sagas
            public class EmptySaga : Saga<EmptySagaData>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<EmptySagaData> mapper)
                {
                }
            }

            public class EmptySagaData : ContainSagaData
            {
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyMessage message)
                {
                    if (Context.RunId != message.RunId)
                        return;

                    Context.Done = true;
                }
            }

            public class MyErrorSubscriber : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public BusNotifications Notifications { get; set; }

                public IBus Bus { get; set; }

                public void Start()
                {
                    unsubscribeStreams.Add(Notifications.Errors.MessageSentToErrorQueue.Subscribe(e =>
                    {
                        Context.Done = true;
                        Context.Error = e.Exception;
                    }));

                    Bus.SendLocal(new MyMessage
                    {
                        RunId = Context.RunId
                    });
                }

                public void Stop()
                {
                    foreach (var unsubscribeStream in unsubscribeStreams)
                    {
                        unsubscribeStream.Dispose();
                    }
                }

                List<IDisposable> unsubscribeStreams = new List<IDisposable>();
            }
        }

        public class MyMessage : ICommand
        {
            public Guid RunId { get; set; }
        }
    }

}
