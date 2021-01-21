namespace Runner
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Features;
    using NServiceBus.Persistence.NHibernate;
    using Saga;


    class Program
    {
        static void Main(string[] args)
        {
            var numberOfThreads = int.Parse(args[0]);
            var volatileMode = args[4].ToLower() == "volatile";
            var suppressDTC = args[4].ToLower() == "suppressdtc";
            var twoPhaseCommit = args[4].ToLower() == "twophasecommit";
            var outbox = args[4].ToLower() == "outbox";
            var saga = args[5].ToLower() == "sagamessages";
            var publish = args[5].ToLower() == "publishmessages";
            var concurrency = int.Parse(args[7]);
            var numberOfMessages = int.Parse(args[1]);

            var endpointName = "PerformanceTest";

            if (volatileMode)
            {
                endpointName += ".Volatile";
            }

            if (suppressDTC)
            {
                endpointName += ".SuppressDTC";
            }

            if (outbox)
            {
                endpointName += ".outbox";
            }

            var config = new EndpointConfiguration(endpointName);
            config.UseTransport<MsmqTransport>().Transactions(suppressDTC ? TransportTransactionMode.SendsAtomicWithReceive : TransportTransactionMode.TransactionScope);
            config.LimitMessageProcessingConcurrencyTo(numberOfThreads);
            config.EnableInstallers();
            config.SendFailedMessagesTo("error");

            switch (args[2].ToLower())
            {
                case "xml":
                    config.UseSerialization<XmlSerializer>();
                    break;

                case "json":
                    config.UseSerialization<NewtonsoftSerializer>();
                    break;

                default:
                    throw new InvalidOperationException("Illegal serialization format " + args[2]);
            }

            config.DisableFeature<Audit>();

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                {
                    new ConnectionStringSettings("NServiceBus/Persistence", SqlServerConnectionString)
                };

            config.UsePersistence<NHibernatePersistence>();
            if (outbox)
            {
                config.EnableOutbox();
            }
            config.EnableFeature<LoaderFeature>();
            config.Recoverability().Immediate(setting => setting.NumberOfRetries(10));
            config.GetSettings().Set(new Loader(async session =>
            {
                if (saga)
                {
                    await SeedSagaMessages(session, numberOfMessages, endpointName, concurrency).ConfigureAwait(false);
                }
                else if (publish)
                {
                    Statistics.PublishTimeNoTx = await PublishEvents(session, numberOfMessages / 2, numberOfThreads, false).ConfigureAwait(false);
                    Statistics.PublishTimeWithTx = await PublishEvents(session, numberOfMessages / 2, numberOfThreads, !outbox).ConfigureAwait(false);
                }
                else
                {
                    Statistics.SendTimeNoTx = await SeedInputQueue(session, numberOfMessages / 2, endpointName, numberOfThreads, false, twoPhaseCommit).ConfigureAwait(false);
                    Statistics.SendTimeWithTx = await SeedInputQueue(session, numberOfMessages / 2, endpointName, numberOfThreads, !outbox, twoPhaseCommit).ConfigureAwait(false);
                }
            }));
            PerformTest(args, config, numberOfMessages).GetAwaiter().GetResult();
        }

        class LoaderFeature : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(new LoaderTask(context.Settings.Get<Loader>()));
            }

            class LoaderTask : FeatureStartupTask
            {
                Loader loader;

                public LoaderTask(Loader loader)
                {
                    this.loader = loader;
                }

                protected override Task OnStart(IMessageSession session)
                {
                    return loader.Load(session);
                }

                protected override Task OnStop(IMessageSession session)
                {
                    return Task.FromResult(0);
                }
            }
        }

        class Loader
        {
            Func<IMessageSession, Task> loadAction;

            public Loader(Func<IMessageSession, Task> loadAction)
            {
                this.loadAction = loadAction;
            }

            public Task Load(IMessageSession session)
            {
                return loadAction(session);
            }
        }

        static async Task PerformTest(string[] args, EndpointConfiguration config, int numberOfMessages)
        {
            var startableBus = await Endpoint.Create(config).ConfigureAwait(false);

            Statistics.StartTime = DateTime.UtcNow;

            await startableBus.Start().ConfigureAwait(false);

            while (Interlocked.Read(ref Statistics.NumberOfMessages) < numberOfMessages)
            {
                Thread.Sleep(1000);
            }

            DumpSetting(args);
            Statistics.Dump();
        }

        static void DumpSetting(string[] args)
        {
            Console.Out.WriteLine("---------------- Settings ----------------");
            Console.Out.WriteLine("Threads: {0}, Serialization: {1}, Transport: {2}, Messagemode: {3}",
                args[0],
                args[2],
                args[3],
                args[5]);
        }

        static async Task SeedSagaMessages(IMessageSession bus, int numberOfMessages, string inputQueue, int concurrency)
        {
            for (var i = 0; i < numberOfMessages / concurrency; i++)
            {
                for (var j = 0; j < concurrency; j++)
                {
                    await bus.Send(inputQueue, new StartSagaMessage
                    {
                        Id = i + 1
                    }).ConfigureAwait(false);
                }
            }
        }

        static async Task<TimeSpan> SeedInputQueue(IMessageSession bus, int numberOfMessages, string inputQueue, int numberOfThreads, bool createTransaction, bool twoPhaseCommit)
        {
            var sw = new Stopwatch();
            sw.Start();
            var tasks = Enumerable.Range(0, numberOfThreads)
                .Select(i => Task.Factory.StartNew(async () =>
           {
               for (var j = 0; j < numberOfMessages / numberOfThreads; i++)
               {
                   var message = CreateMessage();
                   message.TwoPhaseCommit = twoPhaseCommit;
                   message.Id = j + 1;

                   if (createTransaction)
                   {
                       using (var tx = new TransactionScope())
                       {
                           await bus.Send(inputQueue, message).ConfigureAwait(false);
                           tx.Complete();
                       }
                   }
                   else
                   {
                       await bus.Send(inputQueue, message).ConfigureAwait(false);
                   }
               }
           }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);
            sw.Stop();
            return sw.Elapsed;
        }

        static async Task<TimeSpan> PublishEvents(IMessageSession bus, int numberOfMessages, int numberOfThreads, bool createTransaction)
        {
            var sw = new Stopwatch();
            sw.Start();
            var tasks = Enumerable.Range(0, numberOfThreads)
                .Select(i => Task.Factory.StartNew(async () =>
                {
                    for (var j = 0; j < numberOfMessages / numberOfThreads; i++)
                    {
                        if (createTransaction)
                        {
                            using (var tx = new TransactionScope())
                            {
                                await bus.Publish<ITestEvent>().ConfigureAwait(false);
                                tx.Complete();
                            }
                        }
                        else
                        {
                            await bus.Publish<ITestEvent>().ConfigureAwait(false);
                        }
                        Interlocked.Increment(ref Statistics.NumberOfMessages);
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
            await Task.WhenAll(tasks).ConfigureAwait(false);
            sw.Stop();
            return sw.Elapsed;
        }

        static MessageBase CreateMessage()
        {
            return new TestMessage();
        }

        static string SqlServerConnectionString = @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;";
    }
}