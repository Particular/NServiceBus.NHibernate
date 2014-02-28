namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Collections.Generic;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using SagaPersisters.NHibernate.AutoPersistence.Attributes;
    using ScenarioDescriptors;

    public class When_saga_contains_nested_collection_with_row_version : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_persist_correctly()
        {
            Scenario.Define(() => new Context {Id = Guid.NewGuid()})
                      .WithEndpoint<SagaEndpoint>(b => b.Given((bus, context) =>
                      {
                          bus.SendLocal(new Message1
                          {
                              SomeId = context.Id
                          });
                          bus.SendLocal(new Message2
                          {
                              SomeId = context.Id
                          });
                          bus.SendLocal(new Message3
                          {
                              SomeId = context.Id
                          });
                      }))
                    .Done(c => c.SagaCompleted)
                    .Repeat(r => r.For(Transports.Default))
                    .Run();
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

            public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<Message2>, IAmStartedByMessages<Message3>, IAmStartedByMessages<Message1>
            {
                
                public override void ConfigureHowToFindSaga()
                {
                    ConfigureMapping<Message2>(m => m.SomeId).ToSaga(s => s.SomeId);
                    ConfigureMapping<Message3>(m => m.SomeId).ToSaga(s => s.SomeId);
                    ConfigureMapping<Message1>(m => m.SomeId).ToSaga(s => s.SomeId);
                }

                void PerformSagaCompletionCheck()
                {
                    if (Data.RelatedData == null)
                    Data.RelatedData = new List<ChildData>
                                       {
                                           new ChildData{TestSagaData = Data},
                                           new ChildData{TestSagaData = Data},
                                           new ChildData{TestSagaData = Data}
                                       };
                    if (Data.MessageOneReceived && Data.MessageTwoReceived && Data.MessageThreeReceived)
                    {
                        MarkAsComplete();
                        Bus.SendLocal(new SagaCompleted());
                    }
                }

                public void Handle(Message1 message)
                {
                    Data.SomeId = message.SomeId;
                    Data.MessageOneReceived = true;
                    PerformSagaCompletionCheck();
                }
                public void Handle(Message2 message)
                {
                    Data.SomeId = message.SomeId;
                    Data.MessageTwoReceived = true;
                    PerformSagaCompletionCheck();
                }
                public void Handle(Message3 message)
                {
                    Data.SomeId = message.SomeId;
                    Data.MessageThreeReceived = true;
                    PerformSagaCompletionCheck();
                }

            }

        }
        public class CompletionHandler:IHandleMessages<SagaCompleted>
        {
            public Context Context { get; set; }

            public void Handle(SagaCompleted message)
            {
                Context.SagaCompleted = true;
            }
        }
        public class TestSagaData : IContainSagaData
        {
            [RowVersion]
            public virtual byte[] Version { get; set; }
            public virtual Guid Id { get; set; }
            public virtual string Originator { get; set; }
            public virtual string OriginalMessageId { get; set; }
            [Unique]
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
            public virtual TestSagaData TestSagaData { get; set; }
        }
        [Serializable]
        public class SagaCompleted : IMessage
        {
        }
    }

     
}