namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_saga_contains_nested_collection : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_persist_correctly()
        {
            var result = await Scenario.Define<Context>(c => c.Id = Guid.NewGuid())
                .WithEndpoint<NHNestedCollectionEndpoint>(b => b.When(async (bus, context) =>
                {
                    await bus.SendLocal(new Message1
                    {
                        SomeId = context.Id
                    }).ConfigureAwait(false);
                    await bus.SendLocal(new Message2
                    {
                        SomeId = context.Id
                    }).ConfigureAwait(false);
                    await bus.SendLocal(new Message3
                    {
                        SomeId = context.Id
                    }).ConfigureAwait(false);
                }))
                .Done(c => c.SagaCompleted)
                .Run();

            Assert.IsTrue(result.SagaCompleted);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool SagaCompleted { get; set; }
        }

        public class NHNestedCollectionEndpoint : EndpointConfigurationBuilder
        {
            public NHNestedCollectionEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.LimitMessageProcessingConcurrencyTo(1));
            }

            public class NHNestedCollectionSaga : Saga<NHNestedCollectionSagaData>, IAmStartedByMessages<Message2>, IAmStartedByMessages<Message3>, IAmStartedByMessages<Message1>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NHNestedCollectionSagaData> mapper)
                {
                    mapper.ConfigureMapping<Message2>(m => m.SomeId).ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<Message3>(m => m.SomeId).ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<Message1>(m => m.SomeId).ToSaga(s => s.SomeId);
                }

                Task PerformSagaCompletionCheck(IMessageHandlerContext context)
                {
                    if (Data.RelatedData == null)
                        Data.RelatedData = new List<ChildData>
                                       {
                                           new ChildData{NHNestedCollectionSagaData = Data},
                                           new ChildData{NHNestedCollectionSagaData = Data},
                                           new ChildData{NHNestedCollectionSagaData = Data}
                                       };
                    if (Data.MessageOneReceived && Data.MessageTwoReceived && Data.MessageThreeReceived)
                    {
                        MarkAsComplete();
                        return context.SendLocal(new SagaCompleted());
                    }
                    return Task.FromResult(0);
                }

                public Task Handle(Message1 message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;
                    Data.MessageOneReceived = true;
                    return PerformSagaCompletionCheck(context);
                }

                public Task Handle(Message2 message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;
                    Data.MessageTwoReceived = true;
                    return PerformSagaCompletionCheck(context);
                }

                public Task Handle(Message3 message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;
                    Data.MessageThreeReceived = true;
                    return PerformSagaCompletionCheck(context);
                }
            }

        }
        public class CompletionHandler : IHandleMessages<SagaCompleted>
        {
            public Context Context { get; set; }

            public Task Handle(SagaCompleted message, IMessageHandlerContext context)
            {
                Context.SagaCompleted = true;

                return Task.FromResult(0);
            }
        }
        public class NHNestedCollectionSagaData : IContainSagaData
        {
            public virtual Guid Id { get; set; }
            public virtual string Originator { get; set; }
            public virtual string OriginalMessageId { get; set; }
            public virtual Guid SomeId { get; set; }
            public virtual bool MessageTwoReceived { get; set; }
            public virtual bool MessageOneReceived { get; set; }
            public virtual bool MessageThreeReceived { get; set; }
            public virtual IList<ChildData> RelatedData { get; set; }
        }

        [Serializable]
        public class Message2 : IMessage
        {
            public Guid SomeId { get; set; }
        }

        [Serializable]
        public class Message3 : IMessage
        {
            public Guid SomeId { get; set; }
        }

        [Serializable]
        public class Message1 : IMessage
        {
            public Guid SomeId { get; set; }
        }

        public class ChildData
        {
            public virtual Guid Id { get; set; }
            public virtual NHNestedCollectionSagaData NHNestedCollectionSagaData { get; set; }
        }

        [Serializable]
        public class SagaCompleted : IMessage
        {
        }
    }


}