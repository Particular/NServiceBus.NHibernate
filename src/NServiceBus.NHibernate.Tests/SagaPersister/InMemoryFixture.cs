namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Principal;
    using AutoPersistence;
    using global::NHibernate;
    using global::NHibernate.Tool.hbm2ddl;
    using NServiceBus.NHibernate.SharedSession;
    using NUnit.Framework;
    using Saga;

    class InMemoryFixture<T> where T : IContainSagaData
    {
        protected SagaPersister SagaPersister;
        protected ISessionFactory SessionFactory;


        [SetUp]
        public void SetUp()
        {
            connectionString = String.Format(@"Data Source={0};Version=3;New=True;", Path.GetTempFileName());


            var configuration = new global::NHibernate.Cfg.Configuration()
                .AddProperties(new Dictionary<string, string>
                {
                    { "dialect", dialect },
                    { global::NHibernate.Cfg.Environment.ConnectionString,connectionString }
                });

            var modelMapper = new SagaModelMapper(new[] { typeof(T) });

            configuration.AddMapping(modelMapper.Compile());

            SessionFactory = configuration.BuildSessionFactory();

            new SchemaUpdate(configuration).Execute(false, true);

            session = SessionFactory.OpenSession();

            SagaPersister = new SagaPersister(new FakeSessionProvider(session));

            new Installer().Install(WindowsIdentity.GetCurrent().Name);
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

        const string dialect = "NHibernate.Dialect.SQLiteDialect";
        string connectionString;
        ISession session;
        bool sessionFlushed;
    }

    class FakeSessionProvider : IStorageSessionProvider
    {
        public FakeSessionProvider(ISession session)
        {
            Session = session;
        }

        public ISession Session { get; private set; }
    }
}