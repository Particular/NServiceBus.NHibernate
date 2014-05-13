namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.IO;
    using System.Security.Principal;
    using Config.Installer;
    using Config.Internal;
    using global::NHibernate;
    using NUnit.Framework;
    using Persistence.NHibernate;
    using UnitOfWork.NHibernate;

    class InMemoryFixture
    {
        protected SagaPersister SagaPersister;
        protected ISessionFactory SessionFactory;


        [SetUp]
        public void SetUp()
        {
          connectionString = String.Format(@"Data Source={0};Version=3;New=True;", Path.GetTempFileName());

            Configure.ConfigurationSource = new FakeConfigurationSource();

            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                                                               {
                                                                   {"NServiceBus/Persistence/NHibernate/dialect", dialect}
                                                               };

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                                                                     {
                                                                         new ConnectionStringSettings("NServiceBus/Persistence/NHibernate/Saga", connectionString)
                                                                     };

            ConfigureNHibernate.Init();

            Configure.Features.Enable<Features.Sagas>();

            var types = SessionFactoryHelper.Types();

            Configure.With(types)
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .UseNHibernateSagaPersister();

            var builder = new SessionFactoryBuilder(Configure.TypesToScan);
            var properties = ConfigureNHibernate.SagaPersisterProperties;

            SessionFactory = builder.Build(ConfigureNHibernate.CreateConfigurationWith(properties));

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