namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Persistence;
    using NServiceBus.Saga;
    using NUnit.Framework;

    public class When_user_supplies_NH_Configuration : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_use_user_supplied_NH_Configuration_and_connection_string()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<MyEndpoint>(b => b.Given(bus => bus.SendLocal(new Message1
                {
                    SomeId = Guid.NewGuid()
                })))
                .Done(c => c.Completed)
                .Run();

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
                    var cfg = new NHibernate.Cfg.Configuration();
                    cfg.SetProperty(NHibernate.Cfg.Environment.Dialect, typeof(NHibernate.Dialect.MsSql2012Dialect).FullName);
                    cfg.SetProperty(NHibernate.Cfg.Environment.ConnectionDriver, typeof(NHibernate.Driver.Sql2008ClientDriver).FullName);
                    cfg.SetProperty(NHibernate.Cfg.Environment.ConnectionString, @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;");

                    c.UsePersistence<NHibernatePersistence>().UseConfiguration(cfg);
                });
            }

            public class ConfigurePersistence
            {
                public void Configure(BusConfiguration bc)
                {
                    //NOOP - not setting the ConnectionString here to check if it will be picked up from the user-specified Configuration.
                }
            }

            public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<Message1>, IHandleMessages<Message2>
            {
                public Context Context { get; set; }

                public void Handle(Message1 message)
                {
                    Data.SomeId = message.SomeId;
                    Bus.SendLocal(new Message2
                    {
                        SomeId = message.SomeId
                    });
                }

                public void Handle(Message2 _)
                {
                    MarkAsComplete();
                    Context.Completed = true;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
                {
                    mapper.ConfigureMapping<Message1>(m => m.SomeId).ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<Message2>(m => m.SomeId).ToSaga(s => s.SomeId);
                }
            }

            public class TestSagaData : IContainSagaData
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