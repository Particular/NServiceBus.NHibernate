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
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using Persistence.NHibernate;
    using NUnit.Framework;

    [TestFixture(typeof(OutboxRecord), typeof(OutboxRecordMapping))]
    [TestFixture(typeof(GuidOutboxRecord), typeof(GuidOutboxRecordMapping))]
    [TestFixture(typeof(MessageIdOutboxRecord), typeof(MessageIdOutboxRecordMapping))]
    class When_adding_outbox_messages<TEntity, TMapping>
        where TEntity : class, IOutboxRecord, new()
        where TMapping : ClassMapping<TEntity>
    {
        INHibernateOutboxStorage persister;
        ISessionFactory sessionFactory;
        SchemaExport schema;

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
            await schema.CreateAsync(false, true);

            sessionFactory = cfg.BuildSessionFactory();

            persister = new OutboxPersister<TEntity>(sessionFactory, (session, transaction) => new NHibernateOptimisticOutboxTransaction(session, transaction),  "TestEndpoint");
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
            using (var session = sessionFactory.OpenSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    var transportOperations = new[]
                    {
                        new TransportOperation("1", new Dictionary<string, string>(), new byte[0], new Dictionary<string, string>()),
                        new TransportOperation("1", new Dictionary<string, string>(), new byte[0], new Dictionary<string, string>())
                    };

                    await persister.Store(new OutboxMessage(messageId, transportOperations), new NHibernateOptimisticOutboxTransaction(session, transaction), new ContextBag());
                    await transaction.CommitAsync();
                }
            }

            await persister.SetAsDispatched(messageId, new ContextBag());

            var outboxMessage = await persister.Get(messageId, new ContextBag());

            Assert.AreEqual(0, outboxMessage.TransportOperations.Length);
        }

        [Test]
        public async Task Should_throw_if_trying_to_insert_same_messageid()
        {
            var failed = false;
            using (var session = sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                await persister.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), new NHibernateOptimisticOutboxTransaction(session, transaction), new ContextBag());
                await transaction.CommitAsync();
            }
            try
            {
                using (var session = sessionFactory.OpenSession())
                using (var transaction = session.BeginTransaction())
                {
                    await persister.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), new NHibernateOptimisticOutboxTransaction(session, transaction), new ContextBag());
                    await transaction.CommitAsync();
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
            using (var session = sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                await persister.Store(new OutboxMessage(id, new[]
                {
                        new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
                    }), new NHibernateOptimisticOutboxTransaction(session, transaction), new ContextBag());
                await transaction.CommitAsync();
            }

            var result = await persister.Get(id, new ContextBag());
            var operation = result.TransportOperations.Single();

            Assert.AreEqual(id, operation.MessageId);
        }

        [Test]
        public async Task Should_update_dispatched_flag_and_clear_the_operations()
        {
            var id = Guid.NewGuid().ToString("N");

            using (var session = sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                await persister.Store(new OutboxMessage(id, new[]
                {
                    new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
                }), new NHibernateOptimisticOutboxTransaction(session, transaction), new ContextBag());

                await transaction.CommitAsync();
            }

            await persister.SetAsDispatched(id, new ContextBag());

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

            using (var session = sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                await persister.Store(new OutboxMessage("NotDispatched", new TransportOperation[0]), new NHibernateOptimisticOutboxTransaction(session, transaction), new ContextBag());
                await persister.Store(new OutboxMessage(id, new[]
                {
                        new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
                    }), new NHibernateOptimisticOutboxTransaction(session, transaction), new ContextBag());

                await transaction.CommitAsync();
            }

            await persister.SetAsDispatched(id, new ContextBag());

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