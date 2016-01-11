namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class When_building_the_sessionFactory
    {
        [Test]
        public void References_of_the_persistent_entity_should_also_be_mapped()
        {
            var sessionFactory = SessionFactoryHelper.Build(new[]
            {
                typeof(RelatedClass),
                typeof(TestSagaData),
                typeof(TestSaga)
            });
            Assert.NotNull(sessionFactory.GetEntityPersister(typeof (RelatedClass).FullName));
        }
    }
}