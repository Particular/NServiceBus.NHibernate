namespace NServiceBus.NHibernate.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Features;
    using NServiceBus.Persistence;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    public class When_persister_commit_takes_longer_than_dispatch_of_another_later_timeout : NServiceBusAcceptanceTest
    {
        private static string CommandTagHeader = "TestTag.HeaderName";

        [Test]
        public void Should_deliver_timeout()
        {
            var ctx = new Context();

            Scenario.Define(ctx)
                .WithEndpoint<Endpoint>(b =>
                    b.When(bus =>
                    {
                        ctx.CommandATag = Guid.NewGuid().ToString();
                        ctx.CommandBTag = Guid.NewGuid().ToString();

                        var now = DateTime.UtcNow;
                        var deliverCommandAAt = now.AddSeconds(5);
                        var deliverCommandBAt = now.AddSeconds(6);

                        var commandA = new Command();
                        bus.SetMessageHeader(commandA, CommandTagHeader, ctx.CommandATag);
                        bus.Defer(deliverCommandAAt, commandA);

                        var commandB = new Command();
                        bus.SetMessageHeader(commandB, CommandTagHeader, ctx.CommandBTag);
                        bus.Defer(deliverCommandBAt, commandB);
                    }))
                .Done(c => c.CommandAReceived)
                .Run();

            Assert.IsTrue(ctx.CommandAReceived);
        }

        public class Context : ScenarioContext
        {
            public bool CommandAReceived { get; set; }

            public string CommandATag { get; set; }

            public string CommandBTag { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.UsePersistence<DelayingNHibernatePersister>();
                    b.GetSettings().Set("NHibernate.Timeouts.CleanupExecutionInterval", TimeSpan.FromSeconds(10));
                    b.GetSettings().Set("NHibernate.Timeouts.CleanupQueryPeriod", TimeSpan.FromMinutes(10));

                    b.PurgeOnStartup(true);
                });
            }

            public class ProvideConfiguration : IProvideConfiguration<TransportConfig>
            {
                public TransportConfig GetConfiguration()
                {
                    return new TransportConfig
                    {
                        MaximumConcurrencyLevel = 10
                    };
                }
            }

            class Handler : IHandleMessages<Command>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public Context TestContext { get; set; }

                public void Handle(Command message)
                {
                    if (Bus.GetMessageHeader(message, CommandTagHeader) == Context.CommandATag)
                    {
                        TestContext.CommandAReceived = true;
                    }
                }
            }
        }

        [Serializable]
        public class Command : ICommand
        {
        }

        public class DelayingNHibernatePersister : PersistenceDefinition
        {
            public DelayingNHibernatePersister()
            {
                Supports(Storage.Timeouts, s => s.EnableFeatureByDefault<DelayingTimeoutStorage>());
            }
        }

        public class DelayingTimeoutPersister : IPersistTimeouts
        {
            private ManualResetEvent waitWithMessageACommit = new ManualResetEvent(false);

            public Context Context { get; set; }

            public IPersistTimeoutsV2 OriginalPersisterV2 { get; set; }

            private IPersistTimeouts OriginalPersister => (IPersistTimeouts) OriginalPersisterV2;

            public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
            {
                var result = OriginalPersister.GetNextChunk(startSlice, out nextTimeToRunQuery);

                //We want to force Timeout Manager to query more often to make test complete faster
                nextTimeToRunQuery = DateTime.UtcNow.AddSeconds(10);

                return result;
            }

            public void Add(TimeoutData timeout)
            {
                if (timeout.Headers[CommandTagHeader] == Context.CommandATag)
                {
                    waitWithMessageACommit.WaitOne();
                }

                OriginalPersister.Add(timeout);
            }

            public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
            {
                var result = OriginalPersister.TryRemove(timeoutId, out timeoutData);

                if (timeoutData != null &&timeoutData.Headers.ContainsKey(CommandTagHeader) && 
                    timeoutData.Headers[CommandTagHeader] == Context.CommandBTag)
                {
                    waitWithMessageACommit.Set();
                }

                return result;
            }

            public void RemoveTimeoutBy(Guid sagaId)
            {
                OriginalPersister.RemoveTimeoutBy(sagaId);
            }
        }

        public class DelayingTimeoutStorage : NHibernateTimeoutStorage
        {
            public DelayingTimeoutStorage()
            {
                DependsOn<TimeoutManager>();
                DependsOn<NHibernateDBConnectionProvider>();
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
                base.Setup(context);

                context.Container.ConfigureComponent<DelayingTimeoutPersister>(DependencyLifecycle.SingleInstance);
            }
        }
    }
}