namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;

    public class When_using_hbms : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_use_saga_entity_hbm()
        {
            var context = new Context();
            
            Scenario.Define(context)
                      .WithEndpoint<SagaEndpoint>(b => b.Given(bus => bus.SendLocal(new Message1
                      {
                          SomeId = Guid.NewGuid()
                      })))
                    .Done(c => c.SagaCompleted)
                    .Run();

            Assert.IsTrue(context.SagaCompleted);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool SagaCompleted { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<Message1>, IHandleMessages<Message2>
            {
                public Context Context { get; set; }

                public void Handle(Message1 message)
                {
                    Data.SomeId = message.SomeId;
                    Data.LargeText = new string('a', 1000);
                    Bus.SendLocal(new Message2{SomeId = Data.SomeId});
                }

// ReSharper disable once UnusedParameter.Global

                public void Handle(Message2 _)
                {
                    MarkAsComplete();
                    Context.SagaCompleted = true;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
                {
                    mapper.ConfigureMapping<Message1>(m => m.SomeId).ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<Message2>(m => m.SomeId).ToSaga(s => s.SomeId);
                }
            }
        }

        public class TestSagaData : IContainSagaData
        {
            public virtual Guid Id { get; set; }
            public virtual string Originator { get; set; }
            public virtual string OriginalMessageId { get; set; }
            public virtual string LargeText { get; set; }
            [Unique]
            public virtual Guid SomeId { get; set; }
        }

        [Serializable]
        public class Message2 : IMessage
        {
            public Guid SomeId { get; set; }
        }

        [Serializable]
        public class Message1 : IMessage
        {
            public Guid SomeId { get; set; }
        }
    }
}