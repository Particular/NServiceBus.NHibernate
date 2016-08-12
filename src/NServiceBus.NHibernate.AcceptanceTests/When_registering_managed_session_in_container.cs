namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Collections.Generic;
    using EndpointTemplates;
    using AcceptanceTesting;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;
    using NServiceBus.Persistence;
    using NUnit.Framework;
    using NHibernate = global::NHibernate;

    public class When_registering_managed_session_in_container : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_use_user_supplied_NH_Configuration_and_connection_string()
        {
            var context = new Context();
            context.RequestedIds.Add(Guid.NewGuid());
            context.RequestedIds.Add(Guid.NewGuid());

            Scenario.Define(context)
                .WithEndpoint<MyEndpoint>(b => b.Given(bus =>
                {
                    foreach (var id in context.RequestedIds)
                    {
                        bus.SendLocal(new Store
                        {
                            Id = id
                        });
                    }
                    foreach (var id in context.RequestedIds)
                    {
                        bus.SendLocal(new Verify
                        {
                            Id = id
                        });
                    }
                }))
                .Done(c => c.VerifiedIds.Count >= 2)
                .Run();

            CollectionAssert.AreEquivalent(context.RequestedIds, context.VerifiedIds);
        }

        public class Context : ScenarioContext
        {
            public List<Guid> RequestedIds { get; } = new List<Guid>();
            public List<Guid> VerifiedIds { get; } = new List<Guid>();
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

                    var mapper = new ModelMapper();
                    mapper.AddMapping<MyENtityMap>();
                    cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

                    c.UsePersistence<NHibernatePersistence>().UseConfiguration(cfg).RegisterManagedSessionInTheContainer();
                });
            }

            public class ConfigurePersistence
            {
                public void Configure(BusConfiguration bc)
                {
                    //NOOP - not setting the ConnectionString here to check if it will be picked up from the user-specified Configuration.
                }
            }

            class StoreHandler : IHandleMessages<Store>
            {
                public NHibernate.ISession ManagedSession { get; set; }

                public void Handle(Store message)
                {
                    ManagedSession.Save(new MyEntity()
                    {
                        Id = message.Id
                    });
                }
            }

            class VerifyHandler : IHandleMessages<Verify>
            {
                public NHibernate.ISession ManagedSession { get; set; }
                public Context Context { get; set; }

                public void Handle(Verify message)
                {
                    var loaded = ManagedSession.Get<MyEntity>(message.Id);
                    if (loaded != null)
                    {
                        Context.VerifiedIds.Add(loaded.Id);
                    }
                    else
                    {
                        throw new Exception("Expected to fine an entity.");
                    }
                }
            }

            class MyEntity
            {
                public virtual Guid Id { get; set; }
            }

            class MyENtityMap : ClassMapping<MyEntity>
            {
                public MyENtityMap()
                {
                    Table("ManagedSessionInContainer_MyEntity");
                    Id(x => x.Id, id => id.Generator(Generators.Assigned));
                }
            }
        }

        public class Store : ICommand
        {
            public Guid Id { get; set; }
        }

        public class Verify : ICommand
        {
            public Guid Id { get; set; }
        }
    }
}