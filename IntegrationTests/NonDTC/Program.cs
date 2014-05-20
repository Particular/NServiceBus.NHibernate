using System;
using System.Collections.Generic;
using System.Configuration;
using NHibernate.Mapping.ByCode;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Transports;
using Test.NHibernate.Entities;
using Configuration = NHibernate.Cfg.Configuration;
using Environment = NHibernate.Cfg.Environment;

namespace Test.NHibernate
{
    public class Program
    {
        public static void Main()
        {
            var configuration = BuildConfiguration();

            var config = Configure.With()
                .DefaultBuilder()
                .UseTransport<ChaosMonkey>()
                .UseNHibernateTimeoutPersister()
                .UseNHibernateSagaPersister(configuration);

            config.Features.Enable<Sagas>();
            config.Transactions.Advanced(t =>
            {
                t.DisableDistributedTransactions();
                t.DoNotWrapHandlersExecutionInATransactionScope();
            });

            Configure.Component<ChaosMonkeyOutbox>(DependencyLifecycle.SingleInstance);
          
            var bus = config.UnicastBus()
                .CreateBus()
                .Start();

            chaosMonkeyOutbox = config.Builder.Build<ChaosMonkeyOutbox>();


            Guid duplicate = Guid.Parse("1aff989b-a8ec-49f7-85be-a39d54224180");

            Console.Out.WriteLine("Press Enter to place order");
            string s;
            while ((s = Console.ReadLine()) != null)
            {
                switch (s)
                {
                    case "sgo":
                        chaosMonkeyOutbox.SkipGetOnce = true;
                        Console.Out.WriteLine("Monkey: Skip get is now armed");
                        break;

                        
                    case "bad":
                        ChaosMonkeySender.BlowUpAfterDispatch = true;
                        Console.Out.WriteLine("Monkey: BlowUpAfterDispatch is now armed");
                        break;

                    case "fmd":
                          chaosMonkeyOutbox.FailToMarkAsDispatched = true;
                          Console.Out.WriteLine("Monkey: FailToMarkAsDispatched is now armed");
                        break;
                        
                    case "dup":
                        bus.SendLocal<NewOrder>(m =>
                        {
                            m.Product = "duplicate";
                            m.Quantity = 50;
                            m.SetHeader(Headers.MessageId, duplicate.ToString());
                        });
                        break;

                    default:
                        bus.SendLocal(new NewOrder { Product = "Shinny new car", Quantity = 5 });
                        break;
                }


            }

        }

       
        private static Configuration BuildConfiguration()
        {
            var configuration = new Configuration()
                .SetProperties(new Dictionary<string, string>
                {
                    {
                        Environment.ConnectionString,
                        ConfigurationManager.ConnectionStrings["NServiceBus/Persistence"].ConnectionString
                    },
                    {
                        Environment.Dialect,
                        "NHibernate.Dialect.MsSql2012Dialect"
                    }
                });

            var mapper = new ModelMapper();
            mapper.AddMapping<OrderMap>();
            var mappings = mapper.CompileMappingForAllExplicitlyAddedEntities();
            configuration.AddMapping(mappings);
            return configuration;
        }

        static ChaosMonkeyOutbox chaosMonkeyOutbox;
    }

    public class ChaosMonkeyTransport : ConfigureTransport<ChaosMonkey>
    {
        protected override bool RequiresConnectionString
        {
            get { return false; }
        }

        protected override void InternalConfigure(Configure config)
        {
            Enable<MessageDrivenSubscriptions>();

            new MsmqTransport().Initialize(config);

            NServiceBus.Configure.Component<ChaosMonkeySender>(DependencyLifecycle.InstancePerCall);
        }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return ""; }
        }
    }

    public class ChaosMonkey : TransportDefinition
    {
    }

}