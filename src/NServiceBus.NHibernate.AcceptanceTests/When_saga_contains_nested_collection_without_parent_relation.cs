namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Collections.Generic;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_saga_contains_nested_collection_without_parent_relation : NServiceBusAcceptanceTest
    {
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

            public class TestSaga : Saga<TestSagaData>, IHandleMessages<Message2>, IAmStartedByMessages<Message1>
            {
                public void Handle(Message1 message)
                {
                    Data.SomeId = message.SomeId;
                    Data.RelatedData = new List<ChildData>
                    {
                        new ChildData
                        {
                            Name = "Foo1"
                        },
                        new ChildData
                        {
                            Name = "Foo2"
                        },
                        new ChildData
                        {
                            Name = "Foo3"
                        },
                        new ChildData
                        {
                            Name = "Foo4"
                        },
                    };

                    Bus.SendLocal(new Message2
                    {
                        SomeId = message.SomeId
                    });
                }

                public void Handle(Message2 message)
                {
                    MarkAsComplete();
                    Bus.SendLocal(new SagaCompleted());
                }

                public override void ConfigureHowToFindSaga()
                {
                    ConfigureMapping<Message2>(m => m.SomeId).ToSaga(s => s.SomeId);
                    ConfigureMapping<Message1>(m => m.SomeId).ToSaga(s => s.SomeId);
                }
            }
        }

        public class CompletionHandler : IHandleMessages<SagaCompleted>
        {
            public Context Context { get; set; }

            public void Handle(SagaCompleted message)
            {
                Context.SagaCompleted = true;
            }
        }

        public class TestSagaData : IContainSagaData
        {
            [Unique]
            public virtual Guid SomeId { get; set; }
            public virtual IList<ChildData> RelatedData { get; set; }
            public virtual Guid Id { get; set; }
            public virtual string Originator { get; set; }
            public virtual string OriginalMessageId { get; set; }
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

        public class ChildData
        {
            public virtual Guid Id { get; set; }
            public virtual string Name { get; set; }
        }

        [Serializable]
        public class SagaCompleted : IMessage
        {
        }

        [Test]
        public void Should_complete()
        {
            Scenario.Define(() => new Context
            {
                Id = Guid.NewGuid()
            })
                .WithEndpoint<SagaEndpoint>(b => b.Given((bus, context) => bus.SendLocal(new Message1
                {
                    SomeId = context.Id
                })))
                .Done(c => c.SagaCompleted)
                .Repeat(r => r.For(Transports.Default))
                .Run();
        }
    }
}