using System;
using System.Collections.Generic;
using System.Configuration;
using NHibernate.Mapping.ByCode;
using NServiceBus;
using NServiceBus.Features;
using Test.NHibernate.Entities;
using Configuration = NHibernate.Cfg.Configuration;
using Environment = NHibernate.Cfg.Environment;

namespace Test.NHibernate
{
    public class Program
    {
        private static ChaosMonkeyOutbox chaosMonkey;
        public static void Main()
        {
            var configuration = BuildConfiguration();

            Configure.Transactions.Advanced(t => t.DisableDistributedTransactions());
            Configure.Features.Enable<Sagas>();

            var config = Configure.With()
                .DefaultBuilder()
                .UseNHibernateTimeoutPersister()
                .UseNHibernateSagaPersister(configuration)
                .UseNHibernateOutbox(configuration);



            config.Configurer.ConfigureComponent<ChaosMonkeyOutbox>(DependencyLifecycle.SingleInstance);


            var bus = config.UnicastBus()
                .CreateBus()
                .Start();

            chaosMonkey = config.Builder.Build<ChaosMonkeyOutbox>();


            Guid duplicate = Guid.Parse("1aff989b-a8ec-49f7-85be-a39d54224180");

            Console.Out.WriteLine("Press Enter to place order");
            string s;
            while ((s = Console.ReadLine()) != null)
            {
                switch (s)
                {
                    case "sgo":
                        chaosMonkey.SkipGetOnce = true;
                        Console.Out.WriteLine("Monkey: Skip get is now armed");
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
    }
}