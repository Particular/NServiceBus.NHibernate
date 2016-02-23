namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Persistence.NHibernate;
    using NUnit.Framework;

    public class When_using_NHibernate_persistence_without_DTC : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_able_to_retrieve_database_transaction()
        {
            var context = new Context
            {
                Id = Guid.NewGuid()
            };

            Scenario.Define(context)
                    .WithEndpoint<Sender>(b => b.Given((bus, c) =>
                    {
                        bus.Send<MyMessage>(m=>
                        {
                            m.Id = c.Id;
                        });
                    }))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.Received)
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool Received { get; set; }
            
            public Guid Id { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyMessage>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Transactions().DisableDistributedTransactions();
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public NHibernateStorageContext StorageContext { get; set; }

                public void Handle(MyMessage message)
                {
                    if (Context.Id != message.Id)
                        return;

                    Assert.IsNotNull(StorageContext.DatabaseTransaction);
                    Context.Received = true;
                }
            }
        }

        public class MyMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}
