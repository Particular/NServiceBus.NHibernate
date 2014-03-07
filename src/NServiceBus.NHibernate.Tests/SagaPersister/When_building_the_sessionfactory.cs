namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class When_building_the_sessionFactory
    {
        private readonly IDictionary<string, string> testProperties = SQLiteConfiguration.InMemory();

        [Test]
        public void References_of_the_persistent_entity_should_also_be_mapped()
        {
            var sessionFactory = SessionFactoryHelper.Build();

            Assert.NotNull(sessionFactory.GetEntityPersister(typeof (RelatedClass).FullName));
        }
    }
}