namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Driver;
    using Persistence;
    using NUnit.Framework;
    using Environment = global::NHibernate.Cfg.Environment;

    public class When_user_supplies_NH_Configuration : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_user_supplied_NH_Configuration_and_connection_string()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MyEndpoint>(b => b.When(bus => bus.SendLocal(new Message1
                {
                    SomeId = Guid.NewGuid()
                })))
                .Done(c => c.Completed)
                .Run()
                .ConfigureAwait(false);

            Assert.IsTrue(context.Completed);
        }

        public class Context : ScenarioContext
        {
            public bool Completed { get; set; }
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var cfg = new Configuration();
                    cfg.SetProperty(Environment.Dialect, typeof(MsSql2012Dialect).FullName);
                    cfg.SetProperty(Environment.ConnectionDriver, typeof(Sql2008ClientDriver).FullName);
                    cfg.SetProperty(Environment.ConnectionString, ConfigureEndpointNHibernatePersistence.ConnectionString);

                    c.UsePersistence<NHibernatePersistence>().UseConfiguration(cfg);
                });
            }

            public class ConfigurePersistence
            {
                public void Configure(EndpointConfiguration bc)
                {
                    //NOOP - not setting the ConnectionString here to check if it will be picked up from the user-specified Configuration.
                }
            }

            public class Saga13 : Saga<Saga13Data>, IAmStartedByMessages<Message1>, IHandleMessages<Message2>
            {
                public Context Context { get; set; }

                public Task Handle(Message1 message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;
                    return context.SendLocal(new Message2
                    {
                        SomeId = message.SomeId
                    });
                }

                public Task Handle(Message2 _, IMessageHandlerContext context)
                {
                    MarkAsComplete();
                    Context.Completed = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga13Data> mapper)
                {
                    mapper.ConfigureMapping<Message1>(m => m.SomeId).ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<Message2>(m => m.SomeId).ToSaga(s => s.SomeId);
                }
            }

            public class Saga13Data : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
                public virtual Guid SomeId { get; set; }
            }
        }

        [Serializable]
        public class Message1 : IMessage
        {
            public Guid SomeId { get; set; }
        }

        [Serializable]
        public class Message2 : IMessage
        {
            public Guid SomeId { get; set; }
        }
    }
}