namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using Config.Internal;
    using NUnit.Framework;
    using Persistence.NHibernate;

    [TestFixture]
    public class When_configuring_the_saga_persister_from_appconfig
    {
        [SetUp]
        public void SetUp()
        {
            Configure.Features.Enable<Features.Sagas>();

            var types = SessionFactoryHelper.Types();

            Configure.With(types)
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
    }
}