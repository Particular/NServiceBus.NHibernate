namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System.Collections.Specialized;
    using System.Configuration;
    using NUnit.Framework;
    using Persistence.NHibernate;

    [TestFixture]
    public class When_configuring_the_saga_persister_to_use_sqlite
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
                .DefineEndpointName("Foo")
                .DefaultBuilder()
                .UseNHibernateSagaPersister();
        }

        [Test]
        public void Persister_should_be_registered_as_single_call()
        {
            var persister = config.Builder.Build<SagaPersister>();

            Assert.AreNotEqual(persister, config.Builder.Build<SagaPersister>());
        }
    }
}
