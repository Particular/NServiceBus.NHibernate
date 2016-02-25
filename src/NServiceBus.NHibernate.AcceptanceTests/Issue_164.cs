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
    using NServiceBus.Features;
    using NServiceBus.Persistence;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    public class Issue_164 : NServiceBusAcceptanceTest
    {
        private static string CommandTagHeader = "Chaos.HeaderName";

        [Test]
        public void Subscribers_retry_with_SLR()
        {
            var ctx = new Context();

            Scenario.Define(ctx)
                .WithEndpoint<Endpoint>(b =>
                    b.When(bus =>
                    {
                        ctx.MessageATag = "MessageA: " + Guid.NewGuid().ToString();
                        ctx.MessageBTag = "MessageB: " + Guid.NewGuid().ToString();

                        var deliverCommandAAt = DateTime.UtcNow.AddSeconds(5);
                        var deliverCommandBAt = deliverCommandAAt.AddSeconds(1);

                        var commandA = new Command();
                        bus.SetMessageHeader(commandA, CommandTagHeader, ctx.MessageATag);
                        bus.Defer(deliverCommandAAt, commandA);

                        var commandB = new Command();
                        bus.SetMessageHeader(commandB, CommandTagHeader, ctx.MessageBTag);
                        bus.Defer(deliverCommandBAt, commandB);

                    }))
                .Done(c => c.MessageAReceived)
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool MessageAReceived { get; set; }

            public string MessageATag { get; set; }

            public string MessageBTag { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.UsePersistence<ChaosPersister>();
                    b.PurgeOnStartup(true);
                });
            }

            public class ProvideConfiguration : IProvideConfiguration<TransportConfig>
            {
                public TransportConfig GetConfiguration()
                {
                    return new TransportConfig
                    {
                        MaximumConcurrencyLevel = 10,
                        MaximumMessageThroughputPerSecond = 10
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
                    if (Bus.GetMessageHeader(message, CommandTagHeader) == Context.MessageATag)
                    {
                        TestContext.MessageAReceived = true;
                    }
                }
            }
        }

        [Serializable]
        public class Command : ICommand
        {
        }

        public class ChaosPersister : PersistenceDefinition
        {
            public ChaosPersister()
            {
                Supports(Storage.Timeouts, s => s.EnableFeatureByDefault<ChaosTimeoutStorage>());
            }
        }

        public class ChaosTimeoutPersister : IPersistTimeouts
        {
            private ManualResetEvent waitWithMessageACommit = new ManualResetEvent(false);

            public Context Context { get; set; }

            public IPersistTimeoutsV2 OriginalPersisterV2 { get; set; }

            private IPersistTimeouts OriginalPersister => (IPersistTimeouts) OriginalPersisterV2;

            public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
            {
                var result = OriginalPersister.GetNextChunk(startSlice, out nextTimeToRunQuery);

                return result;
            }

            public void Add(TimeoutData timeout)
            {
                if (timeout.Headers[CommandTagHeader] == Context.MessageATag)
                {
                    waitWithMessageACommit.WaitOne();
                }

                OriginalPersister.Add(timeout);
            }

            public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
            {
                var result = OriginalPersister.TryRemove(timeoutId, out timeoutData);

                if (timeoutData != null && timeoutData.Headers[CommandTagHeader] == Context.MessageBTag)
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

        public class ChaosTimeoutStorage : NHibernateTimeoutStorage
        {
            public ChaosTimeoutStorage()
            {
                DependsOn<TimeoutManager>();
                DependsOn<NHibernateDBConnectionProvider>();
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
                base.Setup(context);

                context.Container.ConfigureComponent<ChaosTimeoutPersister>(DependencyLifecycle.SingleInstance);
            }
        }
    }
}