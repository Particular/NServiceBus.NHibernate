namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NHibernate.Cfg;
    using global::NHibernate.Cfg.Loquacious;
    using global::NHibernate.Dialect;
    using global::NHibernate.Driver;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
    using NHibernate.PersistenceTests;
    using NHibernate.SynchronizedStorage;
    using NServiceBus.NHibernate.Outbox;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.SagaPersisters.NHibernate;
    using NServiceBus.SagaPersisters.NHibernate.AutoPersistence;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;
    using Persistence.NHibernate;

    public enum DatabaseVariant
    {
        SqlServer,
        Oracle
    }

    class NHibernateVariant
    {
        public NHibernateVariant(string description, DatabaseVariant databaseVariant, IOutboxPersisterFactory outboxPersisterFactory, bool pessimistic = false, bool transactionScope = false)
        {
            OutboxPersisterFactory = outboxPersisterFactory;
            Description = description;
            DatabaseVariant = databaseVariant;
            Pessimistic = pessimistic;
            TransactionScope = transactionScope;
        }

        public DatabaseVariant DatabaseVariant { get; }

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
        public bool SupportsDtc => true;

        public bool SupportsOutbox => true;

        public bool SupportsFinders => true;

        public bool SupportsPessimisticConcurrency => true;

        public ISagaIdGenerator SagaIdGenerator { get; private set; }

        public ISagaPersister SagaStorage { get; private set; }

        public IOutboxStorage OutboxStorage { get; private set; }

        public Func<ICompletableSynchronizedStorageSession> CreateStorageSession { get; private set; }

        static PersistenceTestsConfiguration()
        {
            var sagaVariants = new List<object>();
            var sqlConnectionString = Consts.ConnectionString;

            if (sqlConnectionString != null)
            {
                sagaVariants.Add(CreateVariant("SQL Server", DatabaseVariant.SqlServer));
            }

            if (!string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("OracleConnectionString")))
            {
                sagaVariants.Add(CreateVariant("Oracle", DatabaseVariant.Oracle));
            }
            SagaVariants = sagaVariants.ToArray();

            var outboxVariants = new List<object>();
            if (sqlConnectionString != null)
            {
                outboxVariants.Add(CreateVariant("SQL Server Optimistic Native default OutboxRecord", DatabaseVariant.SqlServer));
                outboxVariants.Add(CreateVariant("SQL Server Pessimistic Native default OutboxRecord", DatabaseVariant.SqlServer, true, false));
                outboxVariants.Add(CreateVariant("SQL Server Pessimistic TransactionScope default OutboxRecord", DatabaseVariant.SqlServer, true, true));
                outboxVariants.Add(CreateVariant("SQL Server Optimistic TransactionScope default OutboxRecord", DatabaseVariant.SqlServer, false, true));
            }
            OutboxVariants = outboxVariants.ToArray();
        }

        public static void ConfigureDb(DatabaseVariant databaseVariant, DbIntegrationConfigurationProperties props)
        {
            if (databaseVariant == DatabaseVariant.Oracle)
            {
                props.Dialect<Oracle10gDialect>();
                props.Driver<OracleManagedDataClientDriver>();
                props.ConnectionString = System.Environment.GetEnvironmentVariable("OracleConnectionString");
            }
            else if (databaseVariant == DatabaseVariant.SqlServer)
            {
                props.Dialect<MsSql2012Dialect>();
                props.Driver<MicrosoftDataSqlClientDriver>();
                props.ConnectionString = Consts.ConnectionString;
            }
            else
            {
                throw new NotSupportedException("Not supported database variant.");
            }
        }

        static TestFixtureData CreateVariant<T>(string description, DatabaseVariant databaseVariant, bool pessimistic = false, bool transactionScope = false)
            where T : class, IOutboxRecord, new()
        {
            return new TestFixtureData(new TestVariant(new NHibernateVariant(description, databaseVariant, new OutboxPersisterFactory<T>(), pessimistic, transactionScope)));
        }

        static TestFixtureData CreateVariant(string description, DatabaseVariant databaseVariant, bool pessimistic = false, bool transactionScope = false)
        {
            return CreateVariant<OutboxRecord>(description, databaseVariant, pessimistic, transactionScope);
        }

        static bool BelongsToCurrentTest(Type t)
        {
            return t.DeclaringType != null && t.DeclaringType.FullName == TestContext.CurrentContext.Test.ClassName;
        }

        public async Task Configure(CancellationToken cancellationToken = default)
        {
            var variant = (NHibernateVariant)Variant.Values[0];

            var cfg = new Configuration()
                .DataBaseIntegration(x =>
                {
                    ConfigureDb(variant.DatabaseVariant, x);
                });

            //Add mapping for the outbox record
            var mapper = new ModelMapper();
            mapper.AddMapping(typeof(OutboxRecordMapping));
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            var allTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => BelongsToCurrentTest(t)).ToList();
            allTypes.Add(typeof(ContainSagaData));

            var sagaTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
            {
                return BelongsToCurrentTest(t)
                       && (typeof(Saga).IsAssignableFrom(t)
                           || t.GetInterfaces().Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ISagaFinder<,>))
                           || typeof(IFinder).IsAssignableFrom(t));

            }).ToArray();

            sagaMetadataCollection = new SagaMetadataCollection();
            sagaMetadataCollection.Initialize(sagaTypes);

            SagaModelMapper.AddMappings(cfg, SagaMetadataCollection, allTypes, type => ShortenSagaName(type.Name));

            //Mark all map classes as not lazy because they don't declare their properties virtual
            foreach (var mapping in cfg.ClassMappings)
            {
                mapping.IsLazy = false;
            }

            var schema = new SchemaExport(cfg);
            await schema.DropAsync(false, true, cancellationToken);
            await schema.CreateAsync(false, true, cancellationToken);

            var sessionFactory = cfg.BuildSessionFactory();

            SagaIdGenerator = new DefaultSagaIdGenerator();
            SagaStorage = new SagaPersister();
            CreateStorageSession = () => new NHibernateSynchronizedStorageSession(new SessionFactoryHolder(sessionFactory));
            OutboxStorage = variant.OutboxPersisterFactory.Create(sessionFactory, "TestEndpoint", variant.Pessimistic, variant.TransactionScope, IsolationLevel.ReadCommitted, System.Transactions.IsolationLevel.ReadCommitted);
        }

        static string ShortenSagaName(string sagaName)
        {
            return sagaName
                .Replace("AnotherSagaWithCorrelatedProperty", "ASWCP")
                .Replace("SagaWithCorrelationProperty", "SWCP")
                .Replace("SagaWithoutCorrelationProperty", "SWOCP")
                .Replace("SagaWithComplexType", "SWCT")
                .Replace("TestSaga", "TS");
        }

        public Task Cleanup(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}