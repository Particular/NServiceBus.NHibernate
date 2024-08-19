namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate.Impl;
    using global::NHibernate.Persister.Entity;
    using NUnit.Framework;

    [TestFixture]
    public class When_autoMapping_sagas_with_abstract_base_class
    {
        SessionFactoryImpl sessionFactory;

        [SetUp]
        public void SetUp()
        {
            sessionFactory = SessionFactoryHelper.Build(new[]
            {
                typeof(SagaWithAbstractBaseClassActualSaga),
                typeof(SagaWithAbstractBaseClass),
                typeof(ContainSagaData),
                typeof(MyOwnAbstractBase)
            });
        }

        [Test]
        public void Should_not_generate_join_table_for_base_class()
        {
            var persister = sessionFactory.GetEntityPersister(typeof(SagaWithAbstractBaseClass).FullName);
            Assert.That(persister, Is.InstanceOf<UnionSubclassEntityPersister>());
        }

        [Test]
        public void Concrete_class_persister_includes_all_properties_from_abstract_base_classes()
        {
            var persister = sessionFactory.GetEntityPersister(typeof(SagaWithAbstractBaseClass).FullName);
            Assert.That(persister.PropertyNames, Is.EquivalentTo(new[] { "AbstractBaseProp", "CorrelationId", "OrderId", "Originator", "OriginalMessageId" }));
        }
    }

    public class SagaWithAbstractBaseClassActualSaga : Saga<SagaWithAbstractBaseClass>, IAmStartedByMessages<SagaStartMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithAbstractBaseClass> mapper)
        {
            mapper.ConfigureMapping<SagaStartMessage>(m => m.CorrelationId).ToSaga(s => s.CorrelationId);
        }

        public Task Handle(SagaStartMessage message, IMessageHandlerContext context)
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
        public virtual Guid CorrelationId { get; set; }
        public virtual string AbstractBaseProp { get; set; }
    }
}