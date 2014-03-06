namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using AutoPersistence.Attributes;
    using Config.Internal;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    public class When_autoMapping_sagas_with_row_version
    {
        [Test]
        public void Should_throw_if_class_is_derived()
        {
            var builder = new SessionFactoryBuilder(new[] { typeof(MyDerivedClassWithRowVersion) });

            var properties = SQLiteConfiguration.InMemory();

            Assert.Throws<MappingException>(() => builder.Build(new Configuration().AddProperties(properties)));
        }
    }

    public class MyDerivedClassWithRowVersion : ContainSagaData
    {
        [RowVersion]
        public virtual byte[] MyVersion { get; set; }
    }
}