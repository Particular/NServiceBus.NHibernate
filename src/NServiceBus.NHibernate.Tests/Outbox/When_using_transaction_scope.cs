namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System;
    using System.Data;
    using System.Threading.Tasks;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Driver;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
    using NServiceBus.Extensibility;
    using NServiceBus.NHibernate.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using NUnit.Framework;

    [TestFixture(false)]
    [TestFixture(true)]
    class When_using_transaction_scope
    {
        bool pessimistic;
        INHibernateOutboxStorage persister;
        ISessionFactory sessionFactory;
        SchemaExport schema;
        OutboxPersisterFactory<OutboxRecord> outboxPersisterFactory;

        public When_using_transaction_scope(bool pessimistic)
        {
            this.pessimistic = pessimistic;
        }

        [SetUp]
        public async Task Setup()
        {
            var mapper = new ModelMapper();
            mapper.AddMapping(typeof(OutboxRecordMapping));

            var cfg = new Configuration()
                .DataBaseIntegration(x =>
                {
                    x.Dialect<MsSql2012Dialect>();
                    x.Driver<MicrosoftDataSqlClientDriver>();
                    x.ConnectionString = Consts.SqlConnectionString;
                });

            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            schema = new SchemaExport(cfg);
            await schema.DropAsync(false, true);
            await schema.CreateAsync(false, true);

            sessionFactory = cfg.BuildSessionFactory();
            outboxPersisterFactory = new OutboxPersisterFactory<OutboxRecord>();
            persister = outboxPersisterFactory.Create(sessionFactory, "TestEndpoint", pessimistic, true, IsolationLevel.ReadCommitted, System.Transactions.IsolationLevel.ReadCommitted);
        }

        [TearDown]
        public async Task TearDown()
        {
            await sessionFactory.CloseAsync();
            await schema.DropAsync(false, true);
        }

        [Test]
        public async Task Should_allow_the_transaction_to_flow_to_handling_code()
        {
            var messageId = Guid.NewGuid().ToString("N");

            var contextBag = new ContextBag();
            await persister.Get(messageId, contextBag);

            using (var transaction = await persister.BeginTransaction(contextBag))
            {
                var ambientTransaction = System.Transactions.Transaction.Current;

                Assert.IsNotNull(ambientTransaction);

                await transaction.Commit();
            }
        }
    }
}