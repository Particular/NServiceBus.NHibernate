namespace NServiceBus.AcceptanceTests.NonDTC
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Saga;
    using NUnit.Framework;

    public class When_cannot_save_data_to_the_database_using_shared_session : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_retry_processing_of_that_message()
        {
            var context = new Context();
            Scenario.Define(context)
                    .WithEndpoint<NonDtcReceivingEndpoint>(b => b.Given(bus =>
                    {
                        var id = Guid.NewGuid();
                        bus.SendLocal(new StartMessage()
                        {
                            SagaId = id
                        });
                        bus.SendLocal(new TestMessage()
                        {
                            SagaId = id
                        });
                    }))
                    .AllowExceptions()
                    .Done(c => c.GotToSecondAttempt)
                    .Run(TimeSpan.FromSeconds(20));

            Assert.IsTrue(context.GotToSecondAttempt);
            Assert.IsTrue(context.SecondAttempt);
        }



        public class Context : ScenarioContext
        {
            public bool SecondAttempt { get; set; }
            public bool GotToSecondAttempt { get; set; }
        }

        public class NonDtcReceivingEndpoint : EndpointConfigurationBuilder
        {
            public NonDtcReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>(
                    b =>
                    {
                        b.GetSettings().Set("DisableOutboxTransportCheck", true);
                        b.EnableOutbox();
                    });
            }

            public class TestSaga : Saga<TestSagaData>, 
                IAmStartedByMessages<StartMessage>, 
                IHandleMessages<TestMessage>
            {
                public Context Context { get; set; }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
                {
                    mapper.ConfigureMapping<TestMessage>(m => m.SagaId).ToSaga(s => s.SagaId);
                }

                public void Handle(TestMessage message)
                {
                    if (!Context.SecondAttempt)
                    {
                        Data.LongValue = new string('X', 256);                        
                    }
                    else
                    {
                        Context.GotToSecondAttempt = true;
                        MarkAsComplete();
                    }
                    Context.SecondAttempt = true;
                }

                public void Handle(StartMessage message)
                {
                    Data.SagaId = message.SagaId;
                }
            }

            public class TestSagaData : ContainSagaData
            {
                public virtual string LongValue { get; set; }
                public virtual Guid SagaId { get; set; }
            }
        }

        public class StartMessage : IMessage
        {
            public Guid SagaId { get; set; }
        }

        public class TestMessage : IMessage
        {
            public Guid SagaId { get; set; }
        }
    }
}
