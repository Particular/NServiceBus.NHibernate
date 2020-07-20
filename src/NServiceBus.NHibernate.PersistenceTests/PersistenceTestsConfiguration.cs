namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Tool.hbm2ddl;
    using NHibernate.Outbox;
    using NHibernate.PersistenceTests;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.Sagas;
    using Persistence;
    using Persistence.NHibernate;
    using SagaPersisters.NHibernate;
    using SagaPersisters.NHibernate.AutoPersistence;

    public partial class PersistenceTestsConfiguration
    {
        public bool SupportsDtc => false; // TODO: verify if this is true
        public bool SupportsOutbox => true;
        public bool SupportsFinders => true;  // TODO: verify if we actually need this as we think it should only be invoked by core
        public bool SupportsSubscriptions => true;
        public bool SupportsTimeouts => true;
        public bool SupportsPessimisticConcurrency => true;
        public ISagaIdGenerator SagaIdGenerator { get; private set; }
        public ISagaPersister SagaStorage { get; private set; }
        public ISynchronizedStorage SynchronizedStorage { get; private set; }
        public ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; private set; }
        public IOutboxStorage OutboxStorage { get; private set; }

        public async Task Configure()
        {
            CurrentSessionHolder currentSessionHolder = null;
            var outboxPersisterFactory = new OutboxPersisterFactory<OutboxRecord>();

            var cfg = new Configuration()
                .DataBaseIntegration(x =>
                {
                    x.Dialect<MsSql2012Dialect>();
                    x.ConnectionString = Consts.SqlConnectionString;
                });

            var allTypes = Assembly.GetExecutingAssembly().GetTypes().ToList();
            allTypes.Add(typeof(ContainSagaData));
            SagaModelMapper.AddMappings(cfg, SagaMetadataCollection, allTypes, type =>
            {
                //If the type is nested, prefix it with the parent name
                if (type.DeclaringType == null)
                {
                    return type.Name;
                }
                return type.DeclaringType.Name + "_" + type.Name;
            });

            //Mark all map classes as not lazy because they don't declare their properties virtual
            foreach (var mapping in cfg.ClassMappings)
            {
                mapping.IsLazy = false;
            }

            var schema = new SchemaExport(cfg);
            await schema.CreateAsync(false, true);

            var sessionFactory = cfg.BuildSessionFactory();

            SagaIdGenerator = new DefaultSagaIdGenerator();
            SagaStorage = new SagaPersister();
            SynchronizedStorage = new NHibernateSynchronizedStorage(sessionFactory, currentSessionHolder);
            SynchronizedStorageAdapter = new NHibernateSynchronizedStorageAdapter(sessionFactory, currentSessionHolder);
            OutboxStorage = outboxPersisterFactory.Create(sessionFactory, "TestEndpoint", false, false);
        }

        public Task Cleanup()
        {
            return Task.CompletedTask;
        }

        class DefaultSagaIdGenerator : ISagaIdGenerator
        {
            public Guid Generate(SagaIdGeneratorContext context)
            {
                // intentionally ignore the property name and the value.
                return CombGuid.Generate();
            }
        }

        static class CombGuid
        {
            /// <summary>
            /// Generate a new <see cref="Guid" /> using the comb algorithm.
            /// </summary>
            public static Guid Generate()
            {
                var guidArray = Guid.NewGuid().ToByteArray();

                var now = DateTime.UtcNow;

                // Get the days and milliseconds which will be used to build the byte string
                var days = new TimeSpan(now.Ticks - BaseDateTicks);
                var timeOfDay = now.TimeOfDay;

                // Convert to a byte array
                // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333
                var daysArray = BitConverter.GetBytes(days.Days);
                var millisecondArray = BitConverter.GetBytes((long)(timeOfDay.TotalMilliseconds / 3.333333));

                // Reverse the bytes to match SQL Servers ordering
                Array.Reverse(daysArray);
                Array.Reverse(millisecondArray);

                // Copy the bytes into the guid
                Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
                Array.Copy(millisecondArray, millisecondArray.Length - 4, guidArray, guidArray.Length - 4, 4);

                return new Guid(guidArray);
            }

            static readonly long BaseDateTicks = new DateTime(1900, 1, 1).Ticks;
        }
    }
}