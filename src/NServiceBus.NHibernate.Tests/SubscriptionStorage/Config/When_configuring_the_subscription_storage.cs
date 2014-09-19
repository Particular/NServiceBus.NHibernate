namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests.Config
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using NUnit.Framework;
    using Persistence.NHibernate;

    [TestFixture]
    public class When_configuring_the_subscription_storage
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

            config = Configure.With(new Type[] { })
                                .DefineEndpointName("Foo")
                                .DefaultBuilder()
                                .UseNHibernateSubscriptionPersister();
        }

        [Test]
        public void The_session_provider_should_be_registered_as_singleton()
        {
            var sessionSource = config.Builder.Build<ISubscriptionStorageSessionProvider>();

            Assert.AreSame(sessionSource, config.Builder.Build<ISubscriptionStorageSessionProvider>());
        }


        [Test]
        public void The_storage_should_be_registered_as_singlecall()
        {
            var subscriptionStorage = config.Builder.Build<SubscriptionStorage>();

            Assert.AreNotSame(subscriptionStorage, config.Builder.Build<SubscriptionStorage>());
        }
    }
}