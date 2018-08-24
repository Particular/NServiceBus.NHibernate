namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Threading.Tasks;
    using Features;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Impl;
    using global::NHibernate.Persister.Entity;
    using NServiceBus.NHibernate.Tests;
    using Sagas;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class When_autoMapping_sagas_with_abstract_base_class
    {
        SessionFactoryImpl sessionFactory;

        [SetUp]
        public void SetUp()
        {
            var builder = new NHibernateSagaStorage();

            var cfg = new Configuration()
                .DataBaseIntegration(x =>
                {
                    x.Dialect<MsSql2012Dialect>();
                    x.ConnectionString = Consts.SqlConnectionString;
                });

            var types = new[] { typeof(SagaWithAbstractBaseClassActualSaga), typeof(SagaWithAbstractBaseClass), typeof(ContainSagaData), typeof(MyOwnAbstractBase) };

            var allMetadata = new SagaMetadataCollection();
            allMetadata.Initialize(types);

            var settings = new SettingsHolder();
            settings.Set("TypesToScan", types);
            settings.Set(allMetadata);

            builder.ApplyMappings(settings, cfg);
            sessionFactory = cfg.BuildSessionFactory() as SessionFactoryImpl;
        }

        [Test]
        public void Should_not_generate_join_table_for_base_class()
        {
            var persister = sessionFactory.GetEntityPersister(typeof(SagaWithAbstractBaseClass).FullName);
            Assert.IsInstanceOf<UnionSubclassEntityPersister>(persister);
        }

        [Test]
        public void Concrete_class_persister_includes_all_properties_from_abstract_base_classes()
        {
            var persister = sessionFactory.GetEntityPersister(typeof(SagaWithAbstractBaseClass).FullName);
            CollectionAssert.AreEquivalent(new[] { "AbstractBaseProp", "OrderId", "Originator", "OriginalMessageId" }, persister.PropertyNames);
        }
    }

    public class SagaWithAbstractBaseClassActualSaga : Saga<SagaWithAbstractBaseClass>, IAmStartedByMessages<IMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithAbstractBaseClass> mapper)
        {
            mapper.ConfigureMapping<IMessage>(m => m.GetHashCode()).ToSaga(s => s.Id);
        }

        public Task Handle(IMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class SagaWithAbstractBaseClass : MyOwnAbstractBase
    {
        public virtual Guid OrderId { get; set; }
    }

    public abstract class MyOwnAbstractBase : ContainSagaData
    {
        public virtual string AbstractBaseProp { get; set; }
    }
}