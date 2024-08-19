namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_using_hbms : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_saga_entity_hbm()
        {
            var context = await Scenario.Define<Context>()
                      .WithEndpoint<NHUsingHbmsEndpoint>(b => b.When(bus => bus.SendLocal(new Message1 { SomeId = Guid.NewGuid() })))
                    .Done(c => c.SagaCompleted)
                    .Run();

            Assert.That(context.SagaCompleted, Is.True);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool SagaCompleted { get; set; }
        }

        public class NHUsingHbmsEndpoint : EndpointConfigurationBuilder
        {
            public NHUsingHbmsEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class NHUsingHbmsSaga : Saga<NHUsingHbmsSagaData>, IAmStartedByMessages<Message1>, IHandleMessages<Message2>
            {
                Context testContext;

                public NHUsingHbmsSaga(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(Message1 message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;
                    Data.LargeText = new string('a', 1000);
                    return context.SendLocal(new Message2 { SomeId = Data.SomeId });
                }

                public Task Handle(Message2 _, IMessageHandlerContext context)
                {
                    MarkAsComplete();
                    testContext.SagaCompleted = true;

                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NHUsingHbmsSagaData> mapper)
                {
                    mapper.MapSaga(s => s.SomeId)
                        .ToMessage<Message1>(m => m.SomeId)
                        .ToMessage<Message2>(m => m.SomeId);
                }
            }
        }

        public class NHUsingHbmsSagaData : ContainSagaData
        {
            public virtual string LargeText { get; set; }
            public virtual Guid SomeId { get; set; }
            public virtual int Version { get; set; }
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