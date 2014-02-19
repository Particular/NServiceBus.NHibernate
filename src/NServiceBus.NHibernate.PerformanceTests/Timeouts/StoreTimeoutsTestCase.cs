using System;
using System.Threading.Tasks;
using Messages;
using NServiceBus;
using NServiceBus.Features;
using Runner;

public class StoreTimeoutsTestCase : TestCase
{
    public override void Run()
    {
        TransportConfigOverride.MaximumConcurrencyLevel = NumberOfThreads;

        Feature.Disable<Audit>();

        Configure.Transactions.Enable();

        var config = Configure.With()
            .DefineEndpointName("PubSubPerformanceTest")
            .DefaultBuilder()
            .UseTransport<Msmq>()
            .InMemoryFaultManagement()
            .UseNHibernateTimeoutPersister();

        using (var bus = config.UnicastBus()
            .CreateBus())
        {


            Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install();


            Parallel.For(0, NumberMessages, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, x => bus.Defer(TimeSpan.FromDays(1), new DeferredMessage()));


            Statistics.StartTime = DateTime.Now;

            bus.Start();



            Console.ReadLine();

        }

    }
}

namespace Messages
{
    public class DeferredMessage : IMessage
    {
    }    
}

