using System.Data;

namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;
    using global::NHibernate.Tool.hbm2ddl;
    using Extensibility;
    using global::NHibernate.Dialect;
    using NHibernate.Outbox;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using NUnit.Framework;

    [TestFixture(typeof(OutboxRecord), typeof(OutboxRecordMapping), false, false)]
    [TestFixture(typeof(OutboxRecord), typeof(OutboxRecordMapping), true, false)]
    [TestFixture(typeof(OutboxRecord), typeof(OutboxRecordMapping), false, true)]
    [TestFixture(typeof(OutboxRecord), typeof(OutboxRecordMapping), true, true)]
    [TestFixture(typeof(GuidOutboxRecord), typeof(GuidOutboxRecordMapping), false, false)]
    [TestFixture(typeof(GuidOutboxRecord), typeof(GuidOutboxRecordMapping), true, false)]
    [TestFixture(typeof(GuidOutboxRecord), typeof(GuidOutboxRecordMapping), false, true)]
    [TestFixture(typeof(GuidOutboxRecord), typeof(GuidOutboxRecordMapping), true, true)]
    [TestFixture(typeof(MessageIdOutboxRecord), typeof(MessageIdOutboxRecordMapping), false, false)]
    [TestFixture(typeof(MessageIdOutboxRecord), typeof(MessageIdOutboxRecordMapping), true, false)]
    [TestFixture(typeof(MessageIdOutboxRecord), typeof(MessageIdOutboxRecordMapping), false, true)]
    [TestFixture(typeof(MessageIdOutboxRecord), typeof(MessageIdOutboxRecordMapping), true, true)]
    class When_adding_outbox_messages<TEntity, TMapping>
        where TEntity : class, IOutboxRecord, new()
        where TMapping : ClassMapping<TEntity>
    {
        bool pessimistic;
        bool transactionScope;
        INHibernateOutboxStorage persister;
        ISessionFactory sessionFactory;
        SchemaExport schema;
        OutboxPersisterFactory<TEntity> outboxPersisterFactory;

        public When_adding_outbox_messages(bool pessimistic, bool transactionScope)
        {
            this.pessimistic = pessimistic;
            this.transactionScope = transactionScope;
        }

        [SetUp]
        public async Task Setup()
        {
            var mapper = new ModelMapper();
            mapper.AddMapping(typeof(TMapping));

            var cfg = new Configuration()
                .DataBaseIntegration(x =>
                {
                    x.Dialect<MsSql2012Dialect>();
                    x.ConnectionString = Consts.SqlConnectionString;
                });

            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            schema = new SchemaExport(cfg);
            await schema.DropAsync(false, true);
            await schema.CreateAsync(false, true);

            sessionFactory = cfg.BuildSessionFactory();
            outboxPersisterFactory = new OutboxPersisterFactory<TEntity>();
            persister = outboxPersisterFactory.Create(sessionFactory, "TestEndpoint", pessimistic, transactionScope, IsolationLevel.ReadCommitted, System.Transactions.IsolationLevel.ReadCommitted);
        }

        [TearDown]
        public async Task TearDown()
        {
            await sessionFactory.CloseAsync();
            await schema.DropAsync(false, true);
        }

        [Test]
        public async Task Should_not_include_dispatched_transport_operations()
        {
            var messageId = Guid.NewGuid().ToString("N");

            var contextBag = new ContextBag();
            await persister.Get(messageId, contextBag);

            using (var transaction = await persister.BeginTransaction(contextBag))
            {
                var transportOperations = new[]
                {
                    new TransportOperation("1", new Dictionary<string, string>(), new byte[0], new Dictionary<string, string>()),
                    new TransportOperation("1", new Dictionary<string, string>(), new byte[0], new Dictionary<string, string>())
                };

                await persister.Store(new OutboxMessage(messageId, transportOperations), transaction, contextBag);
                await transaction.Commit();
            }

            await persister.SetAsDispatched(messageId, contextBag);

            var outboxMessage = await persister.Get(messageId, contextBag);

            Assert.AreEqual(0, outboxMessage.TransportOperations.Length);
        }

        [Test]
        public async Task Should_throw_if_trying_to_insert_same_messageid()
        {
            var failed = false;

            var contextBag = new ContextBag();
            await persister.Get("MySpecialId", contextBag);

            using (var transactionA = await persister.BeginTransaction(contextBag))
            {
                await persister.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), transactionA, contextBag);
                await transactionA.Commit();
            }

            try
            {
                using (var transactionB = await persister.BeginTransaction(contextBag))
                {
                    await persister.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), transactionB, contextBag);
                    await transactionB.Commit();
                }
            }
            catch (Exception)
            {
                failed = true;
            }
            Assert.IsTrue(failed);
        }

        [Test]
        public async Task Should_save_with_not_dispatched()
        {
            var id = Guid.NewGuid().ToString("N");

            var contextBag = new ContextBag();
            await persister.Get(id, contextBag);

            using (var transaction = await persister.BeginTransaction(contextBag))
            {
                await persister.Store(new OutboxMessage(id, new[]
                {
                    new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
                }), transaction, contextBag);
                await transaction.Commit();
            }

            var result = await persister.Get(id, contextBag);
            var operation = result.TransportOperations.Single();

            Assert.AreEqual(id, operation.MessageId);
        }

        [Test]
        public async Task Should_update_dispatched_flag_and_clear_the_operations()
        {
            var id = Guid.NewGuid().ToString("N");

            var contextBag = new ContextBag();
            await persister.Get(id, contextBag);

            using (var transaction = await persister.BeginTransaction(contextBag))
            {
                await persister.Store(new OutboxMessage(id, new[]
                {
                    new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
                }), transaction, contextBag);

                await transaction.Commit();
            }

            await persister.SetAsDispatched(id, contextBag);

            using (var session = sessionFactory.OpenSession())
            {
                var result = session.QueryOver<IOutboxRecord>().Where(o => o.MessageId == "TestEndpoint/" + id)
                    .SingleOrDefault();

                Assert.True(result.Dispatched);
                Assert.IsNull(result.TransportOperations);
            }
        }

        [Test]
        public async Task Should_delete_all_OutboxRecords_that_have_been_dispatched()
        {
            var id = Guid.NewGuid().ToString("N");
            var dispatchedBag = new ContextBag();

            await persister.Get(id, dispatchedBag);
            using (var transactionA = await persister.BeginTransaction(dispatchedBag))
            {
                await persister.Store(new OutboxMessage(id, new[]
                {
                    new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
                }), transactionA, dispatchedBag);
                await transactionA.Commit();
            }

            await persister.SetAsDispatched(id, dispatchedBag);

            var nonDispatchedBag = new ContextBag();
            await persister.Get("NotDispatched", nonDispatchedBag);
            using (var transactionB = await persister.BeginTransaction(nonDispatchedBag))
            {
                await persister.Store(new OutboxMessage("NotDispatched", new TransportOperation[0]), transactionB, nonDispatchedBag);
                await transactionB.Commit();
            }

            await persister.RemoveEntriesOlderThan(DateTime.UtcNow.AddMinutes(1));

            using (var session = sessionFactory.OpenSession())
            {
                var result = await session.QueryOver<IOutboxRecord>().ListAsync();

                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("TestEndpoint/NotDispatched", result[0].MessageId);
            }
        }
    }
}