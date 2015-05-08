namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_receiving_that_completes_the_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_hydrate_and_complete_the_existing_instance()
        {
            Scenario.Define(() => new Context
            {
                Id = Guid.NewGuid()
            })
                .WithEndpoint<SagaEndpoint>(b =>
                {
                    b.Given((bus, context) => bus.SendLocal(new StartSagaMessage
                    {
                        SomeId = context.Id
                    }));
                    b.When(context => context.StartSagaMessageReceived, (bus, context) =>
                    {
                        context.AddTrace("CompleteSagaMessage sent");

                        bus.SendLocal(new CompleteSagaMessage
                        {
                            SomeId = context.Id
                        });
                    });
                })
                .Done(c => c.SagaCompleted)
                .Repeat(r => r.For(Transports.Default))
                .Should(c => Assert.True(c.SagaCompleted))

                .Run();
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool StartSagaMessageReceived { get; set; }
            public bool SagaCompleted { get; set; }
            public bool AnotherMessageReceived { get; set; }
            public bool SagaReceivedAnotherMessage { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(b => b.LoadMessageHandlers<First<TestSaga>>());
            }

            public class TestSaga : Saga<TestSagaData>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<CompleteSagaMessage>,
                IHandleSagaNotFound
            {
                public Context Context { get; set; }

                public void Handle(StartSagaMessage message)
                {
                    Context.AddTrace("Saga started");

                    Data.SomeId = message.SomeId;

                    Context.StartSagaMessageReceived = true;
                }

                public void Handle(CompleteSagaMessage message)
                {
                    Context.AddTrace("CompleteSagaMessage received");
                    MarkAsComplete();
                    Context.SagaCompleted = true;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<CompleteSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public void Handle(object message)
                {
                    throw new Exception("Unexpected 'saga not found' for message: " + message.GetType().Name);
                }
            }

            public class TestSagaData : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
                [Unique]
                public virtual Guid SomeId { get; set; }
            }
        }

        [Serializable]
        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        [Serializable]
        public class CompleteSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}