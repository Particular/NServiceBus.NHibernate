namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using Features;
    using global::NHibernate;
    using global::NHibernate.Cfg;
    using NUnit.Framework;
    using Saga;
    using Settings;

    [TestFixture]
    public class When_autoMapping_sagas_with_row_version
    {
        [Test]
        public void Should_throw_if_class_is_derived()
        {
            var builder = new NHibernateSagaStorage();
            var properties = SQLiteConfiguration.InMemory();

            var configuration = new Configuration().AddProperties(properties);
            var settings = new SettingsHolder();
            settings.Set("TypesToScan", new[] { typeof(MyDerivedClassWithRowVersion) });
            
            Assert.Throws<MappingException>(() => builder.ApplyMappings(settings, configuration));
        }
    }

    public class MyDerivedClassWithRowVersion : ContainSagaData
    {
        [RowVersion]
        public virtual byte[] MyVersion { get; set; }
    }
}