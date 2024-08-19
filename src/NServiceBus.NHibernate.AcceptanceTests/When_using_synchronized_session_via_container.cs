namespace NServiceBus.AcceptanceTests.Sagas
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using global::NHibernate;
    using Microsoft.Extensions.DependencyInjection;

    [TestFixture]
    public class When_using_synchronized_session_via_container : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_inject_synchronized_session_into_handler()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(s => s.SendLocal(new MyMessage())))
                .Done(c => c.Done)
                .Run()
                .ConfigureAwait(false);

            Assert.IsNotNull(context.SessionInjectedToFirstHandler);
            Assert.IsNotNull(context.SessionInjectedToSecondHandler);
            Assert.IsNotNull(context.SessionInjectedToThirdHandler);
            Assert.That(context.SessionInjectedToSecondHandler, Is.SameAs(context.SessionInjectedToFirstHandler));
            Assert.AreNotSame(context.SessionInjectedToFirstHandler, context.SessionInjectedToThirdHandler);
        }

        public class Context : ScenarioContext
        {
            public ISession SessionInjectedToFirstHandler { get; set; }
            public ISession SessionInjectedToSecondHandler { get; set; }
            public ISession SessionInjectedToThirdHandler { get; set; }
            public bool Done { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.RegisterComponents(cc =>
                    {
                        cc.AddScoped(b => b.GetRequiredService<INHibernateStorageSession>().Session);
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
                    return handlerContext.SendLocal(new FollowUpMessage
                    {
                        Property = message.Property
                    });
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

            public class FollowUpMessageMessageHandler : IHandleMessages<FollowUpMessage>
            {
                Context context;
                ISession session;

                public FollowUpMessageMessageHandler(ISession session, Context context)
                {
                    this.context = context;
                    this.session = session;
                }


                public Task Handle(FollowUpMessage message, IMessageHandlerContext handlerContext)
                {
                    context.SessionInjectedToThirdHandler = session;
                    context.Done = true;
                    return Task.CompletedTask;
                }
            }
        }

        public class MyMessage : IMessage
        {
            public string Property { get; set; }
        }

        public class FollowUpMessage : IMessage
        {
            public string Property { get; set; }
        }

    }
}