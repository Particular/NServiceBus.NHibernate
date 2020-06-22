using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

namespace NServiceBus.AcceptanceTests.Sagas
{
    using global::NHibernate;

    [TestFixture]
    public class When_using_synchronized_session_via_container : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_inject_synchronized_session_into_handler()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(s => s.SendLocal(new MyMessage())))
                .Done(c => c.SessionInjectedToFirstHandler != null && c.SessionInjectedToSecondHandler != null)
                .Run()
                .ConfigureAwait(false);

            Assert.IsNotNull(context.SessionInjectedToFirstHandler);
            Assert.IsNotNull(context.SessionInjectedToSecondHandler);
            Assert.AreSame(context.SessionInjectedToFirstHandler, context.SessionInjectedToSecondHandler);
        }

        public class Context : ScenarioContext
        {
            public ISession SessionInjectedToFirstHandler { get; set; }
            public ISession SessionInjectedToSecondHandler { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.RegisterComponents(cc =>
                    {
                        cc.ConfigureComponent(b => b.Build<INHibernateStorageSession>().Session, DependencyLifecycle.InstancePerUnitOfWork);
                    });
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                Context context;
                ISession session;

                public MyMessageHandler(ISession session, Context context)
                {
                    this.context = context;
                    this.session = session;
                }


                public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
                {
                    context.SessionInjectedToFirstHandler = session;
                    return Task.CompletedTask;
                }
            }

            public class MyOtherMessageHandler : IHandleMessages<MyMessage>
            {
                Context context;
                ISession session;

                public MyOtherMessageHandler(ISession session, Context context)
                {
                    this.context = context;
                    this.session = session;
                }


                public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
                {
                    context.SessionInjectedToSecondHandler = session;
                    return Task.CompletedTask;
                }
            }
        }

        public class MyMessage : IMessage
        {
            public string Property { get; set; }
        }

    }
}