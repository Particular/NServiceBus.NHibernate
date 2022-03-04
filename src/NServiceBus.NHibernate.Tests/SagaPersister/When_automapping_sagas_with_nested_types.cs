namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::NHibernate.Impl;
    using NUnit.Framework;

    [TestFixture]
    public class When_autoMapping_sagas_with_nested_types
    {
        SessionFactoryImpl sessionFactory;

        [SetUp]
        public void SetUp()
        {
            sessionFactory = SessionFactoryHelper.Build(
                new[]
                {
                    typeof(TestSaga2),
                    typeof(TestSaga2ActualSaga),
                    typeof(ContainSagaData),
                    typeof(SagaWithNestedTypeActualSaga),
                    typeof(SagaWithNestedType),
                    typeof(SagaWithNestedType.Customer),
                    typeof(SagaWithNestedSagaData),
                    typeof(SagaWithNestedSagaData.NestedSagaData)
                });
        }

        [Test]
        public void Table_name_for_nested_entity_should_be_generated_correctly()
        {
            var persister = sessionFactory.GetEntityPersister(typeof(SagaWithNestedType.Customer).FullName).
                   ClassMetadata as global::NHibernate.Persister.Entity.AbstractEntityPersister;

            Assert.AreEqual("SagaWithNestedType_Customer", persister.TableName);
        }

        [Test]
        public void Table_name_for_nested_saga_data_should_be_the_parent_saga()
        {
            var persister = sessionFactory.GetEntityPersister(typeof(SagaWithNestedSagaData.NestedSagaData).FullName).
                   ClassMetadata as global::NHibernate.Persister.Entity.AbstractEntityPersister;

            Assert.AreEqual("SagaWithNestedSagaData", persister.TableName);
        }

        [Test]
        public void Table_name_for_nested_saga_data_that_derives_from_abstract_class_should_be_the_parent_saga()
        {
            var persister = sessionFactory.GetEntityPersister(typeof(TestSaga2).FullName).
                   ClassMetadata as global::NHibernate.Persister.Entity.AbstractEntityPersister;

            Assert.AreEqual("When_autoMapping_sagas_with_nested_types", persister.TableName);
        }

        public class TestSaga2 : ContainSagaData
        {
            public virtual Guid SagaId { get; set; }
        }

        public class TestSaga2ActualSaga : Saga<TestSaga2>, IAmStartedByMessages<SagaStartMessage>
        {

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSaga2> mapper)
            {
                mapper.ConfigureMapping<SagaStartMessage>(m => m.SagaId).ToSaga(s => s.SagaId);
            }

            public Task Handle(SagaStartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }
        }
    }

    public class SagaWithNestedType : ContainSagaData
    {
        public virtual Guid SagaId { get; set; }
        public virtual IList<Customer> Customers { get; set; }

        public class Customer
        {
            public virtual Guid Id { get; set; }
            public virtual string Name { get; set; }
        }
    }

    public class SagaWithNestedTypeActualSaga : Saga<SagaWithNestedType>, IAmStartedByMessages<SagaStartMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithNestedType> mapper)
        {
            mapper.ConfigureMapping<SagaStartMessage>(m => m.SagaId).ToSaga(s => s.SagaId);
        }

        public Task Handle(SagaStartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class SagaWithNestedSagaData : Saga<SagaWithNestedSagaData.NestedSagaData>, IAmStartedByMessages<SagaStartMessage>
    {
        public class NestedSagaData : ContainSagaData
        {
            public virtual Guid SagaId { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NestedSagaData> mapper)
        {
            mapper.ConfigureMapping<SagaStartMessage>(m => m.SagaId).ToSaga(s => s.SagaId);
        }

        public Task Handle(SagaStartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }
}