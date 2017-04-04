namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Principal;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using NServiceBus.Persistence.NHibernate;
    using NServiceBus.Saga;
    using NServiceBus.SagaPersisters.NHibernate.AutoPersistence;
    using NServiceBus.TimeoutPersisters.NHibernate.Installer;
    using NUnit.Framework;
    using Environment = global::NHibernate.Cfg.Environment;
    using Installer = NServiceBus.Persistence.NHibernate.Installer;

    class InMemoryFixture<T> where T : IContainSagaData
    {
        const string dialect = "NHibernate.Dialect.SQLiteDialect";


        [SetUp]
        public void SetUp()
        {
            connectionString = $@"Data Source={Path.GetTempFileName()};New=True;";

            var configuration = new Configuration()
                .AddProperties(new Dictionary<string, string>
                {
                    {"dialect", dialect},
                    {Environment.ConnectionString, connectionString}
                });

            var modelMapper = new SagaModelMapper(new[]
            {
                typeof(T)
            });

            configuration.AddMapping(modelMapper.Compile());

            SessionFactory = configuration.BuildSessionFactory();

            new OptimizedSchemaUpdate(configuration).Execute(false, true);

            session = SessionFactory.OpenSession();

            SagaPersister = new SagaPersister(new FakeSessionProvider(SessionFactory, session));

            new Installer().Install(WindowsIdentity.GetCurrent().Name, null);
        }

        protected void FlushSession()
        {
            if (sessionFlushed)
            {
                return;
            }

            sessionFlushed = true;

            session.Flush();
        }

        [TearDown]
        public void Cleanup()
        {
            FlushSession();
            SessionFactory.Close();
        }

        string connectionString;
        protected SagaPersister SagaPersister;
        ISession session;
        protected ISessionFactory SessionFactory;
        bool sessionFlushed;
    }

    class FakeSessionProvider : IStorageSessionProvider
    {
        public FakeSessionProvider(ISessionFactory sessionFactory, ISession session)
        {
            this.sessionFactory = sessionFactory;
            Session = session;
        }

        public ISession Session { get; private set; }

        public void ExecuteInTransaction(Action<ISession> operation)
        {
            operation(Session);
        }

        public IStatelessSession OpenStatelessSession()
        {
            return sessionFactory.OpenStatelessSession();
        }

        public ISession OpenSession()
        {
            return sessionFactory.OpenSession();
        }

        readonly ISessionFactory sessionFactory;
    }
}