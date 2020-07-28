﻿namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::NHibernate.Cfg;
    using global::NHibernate.Cfg.Loquacious;
    using global::NHibernate.Dialect;
    using global::NHibernate.Driver;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
    using NHibernate.Outbox;
    using NHibernate.PersistenceTests;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;
    using Persistence.NHibernate;
    using SagaPersisters.NHibernate;
    using SagaPersisters.NHibernate.AutoPersistence;

    class NHibernateVariant
    {
        public NHibernateVariant(string description, Action<IDbIntegrationConfigurationProperties> configureDb, IOutboxPersisterFactory outboxPersisterFactory, bool pessimistic = false, bool transactionScope = false)
        {
            ConfigureDb = configureDb;
            OutboxPersisterFactory = outboxPersisterFactory;
            Description = description;
            Pessimistic = pessimistic;
            TransactionScope = transactionScope;
        }

        public Action<IDbIntegrationConfigurationProperties> ConfigureDb { get; }
        public IOutboxPersisterFactory OutboxPersisterFactory { get; }
        public bool Pessimistic { get; }
        public bool TransactionScope { get; }
        public string Description { get; }

        public override string ToString()
        {
            return Description;
        }
    }

    public partial class PersistenceTestsConfiguration
    {
        public PersistenceTestsConfiguration()
        {

        }

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

        static PersistenceTestsConfiguration()
        {
            SagaVariants = new[]
            {
                CreateVariant("SQL Server", x =>
                {
                    x.Dialect<MsSql2012Dialect>();
                    x.ConnectionString = Consts.ConnectionString;
                }),
                CreateVariant("Oracle", x =>
                {
                    x.Dialect<Oracle10gDialect>();
                    x.Driver<OracleManagedDataClientDriver>();
                    x.ConnectionString = System.Environment.GetEnvironmentVariable("OracleConnectionString");
                })
            };
            OutboxVariants = new[]
            {
                CreateVariant("SQL Server Optimistic Native default OutboxRecord", x =>
                {
                    x.Dialect<MsSql2012Dialect>();
                    x.ConnectionString = Consts.ConnectionString;
                }),
                CreateVariant("SQL Server Pessimistic Native default OutboxRecord", x =>
                {
                    x.Dialect<MsSql2012Dialect>();
                    x.ConnectionString = Consts.ConnectionString;
                }, true, false),
                CreateVariant("SQL Server Pessimistic TransactionScope default OutboxRecord", x =>
                {
                    x.Dialect<MsSql2012Dialect>();
                    x.ConnectionString = Consts.ConnectionString;
                }, true, true),
                CreateVariant("SQL Server Optimistic TransactionScope default OutboxRecord", x =>
                {
                    x.Dialect<MsSql2012Dialect>();
                    x.ConnectionString = Consts.ConnectionString;
                }, false, true),
            };
        }

        static TestFixtureData CreateVariant<T>(string description, Action<IDbIntegrationConfigurationProperties> configureDb, bool pessimistic = false, bool transactionScope = false) 
            where T : class, IOutboxRecord, new()
        {
            return new TestFixtureData(new TestVariant(new NHibernateVariant(description, configureDb, new OutboxPersisterFactory<T>(), pessimistic, transactionScope)));
        }

        static TestFixtureData CreateVariant(string description, Action<IDbIntegrationConfigurationProperties> configureDb, bool pessimistic = false, bool transactionScope = false)
        {
            return CreateVariant<OutboxRecord>(description, configureDb, pessimistic, transactionScope);
        }

        public async Task Configure()
        {
            var variant = (NHibernateVariant) Variant.Values[0];

            var cfg = new Configuration()
                .DataBaseIntegration(variant.ConfigureDb);

            //Add mapping for the outbox record
            var mapper = new ModelMapper();
            mapper.AddMapping(typeof(OutboxRecordMapping));
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

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
            SynchronizedStorage = new NHibernateSynchronizedStorage(sessionFactory, null);
            SynchronizedStorageAdapter = new NHibernateSynchronizedStorageAdapter(sessionFactory, null);
            OutboxStorage = variant.OutboxPersisterFactory.Create(sessionFactory, "TestEndpoint", variant.Pessimistic, variant.TransactionScope);
        }

        public Task Cleanup()
        {
            return Task.CompletedTask;
        }
    }
}