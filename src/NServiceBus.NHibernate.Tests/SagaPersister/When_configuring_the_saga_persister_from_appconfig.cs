namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Linq;
    using Config.Internal;
    using NUnit.Framework;
    using Persistence.NHibernate;
    using UnitOfWork.NHibernate;

    [TestFixture]
    public class When_configuring_the_saga_persister_from_appconfig
    {
        private Configure config;

        [SetUp]
        public void SetUp()
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection();
            NHibernateSettingRetriever.ConnectionStrings = () => new ConnectionStringSettingsCollection
            {
                new ConnectionStringSettings("NServiceBus/Persistence", @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;")
            };
            ConfigureNHibernate.Init();

            Configure.Features.Enable<Features.Sagas>();

            var types = SessionFactoryHelper.Types();

            config = Configure.With(types)
                .DefineEndpointName("xyz")
                .DefaultBuilder()
                .UseNHibernateSagaPersister();
        }

        [Test]
        public void Update_schema_can_be_specified_by_the_user()
        {
            var builder = new SessionFactoryBuilder(Configure.TypesToScan);
            var properties = ConfigureNHibernate.SagaPersisterProperties;

            var sessionFactory = builder.Build(ConfigureNHibernate.CreateConfigurationWith(properties));

            using (var session = sessionFactory.OpenSession())
            {
                session.CreateCriteria(typeof(MySaga)).List<MySaga>();
            }
        }

        [Test]
        public void UnitOfWork_Should_be_configured()
        {
            var uow = config.Builder.Build<UnitOfWorkManager>();

            Assert.IsNotNull(uow);
        }

        [Test]
        public void Handles_Multiple_registrations_of_UnitOfWork()
        {
            var uow = config.Builder.BuildAll<UnitOfWorkManager>().ToList();

            Assert.IsNotNull(uow);
            Assert.That(uow, Has.Count.EqualTo(1));
        }
    }
}