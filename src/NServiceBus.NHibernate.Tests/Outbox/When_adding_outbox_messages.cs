namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;
    using global::NHibernate.Tool.hbm2ddl;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.Persistence.NHibernate;
    using NUnit.Framework;
    using Environment = global::NHibernate.Cfg.Environment;

    [TestFixture(typeof(OutboxRecord), typeof(OutboxRecordMapping))]
    [TestFixture(typeof(GuidOutboxRecord), typeof(GuidOutboxRecordMapping))]
    [TestFixture(typeof(MessageIdOutboxRecord), typeof(MessageIdOutboxRecordMapping))]
    class When_adding_outbox_messages<TEntity, TMapping>
        where TEntity : class, IOutboxRecord, new()
        where TMapping : ClassMapping<TEntity>
    {

#if USE_SQLSERVER
        private readonly string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True;";
        private const string dialect = "NHibernate.Dialect.MsSql2012Dialect";
#else
        string connectionString = $@"Data Source={Path.GetTempFileName()};Version=3;New=True;";
        const string dialect = "NHibernate.Dialect.SQLiteDialect";
#endif

        INHibernateOutboxStorage persister;
        ISessionFactory sessionFactory;

        [SetUp]
        public void Setup()
        {
            var mapper = new ModelMapper();
            mapper.AddMapping(typeof(TMapping));

            var configuration = new Configuration()
                .AddProperties(new Dictionary<string, string>
                {
                    {"dialect", dialect},
                    {Environment.ConnectionString, connectionString}
                });

            configuration.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            new SchemaUpdate(configuration).Execute(false, true);

            sessionFactory = configuration.BuildSessionFactory();

            persister = new OutboxPersister<TEntity>(sessionFactory, "TestEndpoint");
        }

        [TearDown]
        public void TearDown()
        {
            sessionFactory.Dispose();
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

                    await persister.Store(new OutboxMessage(messageId, transportOperations), new NHibernateOutboxTransaction(session, transaction), new ContextBag());
                    transaction.Commit();
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
                await persister.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), new NHibernateOutboxTransaction(session, transaction), new ContextBag());
                transaction.Commit();
            }
            try
            {
                using (var session = sessionFactory.OpenSession())
                using (var transaction = session.BeginTransaction())
                {
                    await persister.Store(new OutboxMessage("MySpecialId", new TransportOperation[0]), new NHibernateOutboxTransaction(session, transaction), new ContextBag());
                    transaction.Commit();
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
                    }), new NHibernateOutboxTransaction(session, transaction), new ContextBag());
                transaction.Commit();
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
                }), new NHibernateOutboxTransaction(session, transaction), new ContextBag());

                transaction.Commit();
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
                await persister.Store(new OutboxMessage("NotDispatched", new TransportOperation[0]), new NHibernateOutboxTransaction(session, transaction), new ContextBag());
                await persister.Store(new OutboxMessage(id, new[]
                {
                        new TransportOperation(id, new Dictionary<string, string>(), new byte[1024*5], new Dictionary<string, string>()),
                    }), new NHibernateOutboxTransaction(session, transaction), new ContextBag());

                transaction.Commit();
            }

            await persister.SetAsDispatched(id, new ContextBag());

            persister.RemoveEntriesOlderThan(DateTime.UtcNow.AddMinutes(1));

            using (var session = sessionFactory.OpenSession())
            {
                var result = session.QueryOver<IOutboxRecord>().List();

                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("TestEndpoint/NotDispatched", result[0].MessageId);
            }
        }
    }
}