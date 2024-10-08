﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using global::NHibernate;
    using Microsoft.Extensions.DependencyInjection;

    [TestFixture]
    public class When_using_outbox_synchronized_session_via_container : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_inject_synchronized_session_into_handler()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(s => s.SendLocal(new MyMessage())))
                .Done(c => c.Done)
                .Run()
                .ConfigureAwait(false);

            Assert.That(context.RepositoryHasSession, Is.True);
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public bool RepositoryHasSession { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                    c.EnableOutbox();
                    c.RegisterComponents(cc =>
                    {
                        cc.AddScoped<MyRepository>();
                        cc.AddScoped(b => b.GetRequiredService<INHibernateStorageSession>().Session);
                    });
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                Context context;
                MyRepository repository;

                public MyMessageHandler(MyRepository repository, Context context)
                {
                    this.context = context;
                    this.repository = repository;
                }


                public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
                {
                    repository.DoSomething();
                    context.Done = true;
                    return Task.CompletedTask;
                }
            }
        }

        public class MyRepository
        {
            ISession session;
            Context context;

            public MyRepository(ISession session, Context context)
            {
                this.session = session;
                this.context = context;
            }

            public void DoSomething()
            {
                context.RepositoryHasSession = session != null;
            }
        }

        public class MyMessage : IMessage
        {
            public string Property { get; set; }
        }

    }
}