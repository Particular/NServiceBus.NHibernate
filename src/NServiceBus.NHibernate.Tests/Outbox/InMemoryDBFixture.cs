//#define USE_SQLSERVER

namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using global::NHibernate;
    using Pipeline;
#if !USE_SQLSERVER
    using System.IO;
#endif
    using System.Linq;
    using System.Security.Principal;
    using Config.ConfigurationSource;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using NUnit.Framework;
    using Persistence.NHibernate;


    abstract class InMemoryDBFixture
    {
        protected OutboxPersister persister;

#if USE_SQLSERVER
        private readonly string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True;";
        private const string dialect = "NHibernate.Dialect.MsSql2012Dialect";
#else
        private readonly string connectionString = String.Format(@"Data Source={0};Version=3;New=True;", Path.GetTempFileName());
        private const string dialect = "NHibernate.Dialect.SQLiteDialect";
#endif

        [SetUp]
        public void Setup()
        {
            Configure.ConfigurationSource = new DefaultConfigurationSource();

            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
                                                               {
                                                                   {"NServiceBus/Persistence/NHibernate/dialect", dialect}
                                                               };

            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
                                                                     {
                                                                         new ConnectionStringSettings("NServiceBus/Persistence/NHibernate/Outbox", connectionString)
                                                                     };

            ConfigureNHibernate.Init();

            Configure.With(Enumerable.Empty<Type>())
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .UseNHibernateOutbox();

            persister = Configure.Instance.Builder.Build<OutboxPersister>();
            SessionFactory = persister.SessionFactory;

            persister.PipelineExecutor = new PipelineExecutor(Configure.Instance.Builder, new PipelineBuilder(Configure.Instance.Builder));


            var connection = SessionFactory.GetConnection();

            persister.PipelineExecutor.CurrentContext.Set("SqlConnection-" + connectionString, connection);

            new Installer().Install(WindowsIdentity.GetCurrent().Name);
        }

        protected ISessionFactory SessionFactory;
    }
}
