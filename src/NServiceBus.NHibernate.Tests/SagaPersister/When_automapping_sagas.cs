namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Linq;
    using global::NHibernate.Engine;
    using global::NHibernate.Id;
    using global::NHibernate.Impl;
    using global::NHibernate.Persister.Entity;
    using NUnit.Framework;

    [TestFixture]
    public class When_autoMapping_sagas
    {
        IEntityPersister persisterForTestSaga;
        SessionFactoryImpl sessionFactory;

        [OneTimeSetUp]
        public void SetUp()
        {
            var types = new[]
            {
                typeof(TestSaga), typeof(TestSagaData), typeof(TestComponent), typeof(PolymorphicPropertyBase),
                typeof(AlsoDerivedFromSagaWithTableName), typeof(AlsoDerivedFromSagaWithTableNameData),
                typeof(DerivedFromSagaWithTableName), typeof(DerivedFromSagaWithTableNameData),
                typeof(SagaWithTableName), typeof(SagaWithTableNameData),
                typeof(SagaWithVersionedProperty), typeof(SagaWithVersionedPropertyData),
                typeof(SagaWithoutVersionedProperty), typeof(SagaWithoutVersionedPropertyData),
                typeof(ContainSagaData), typeof(object)
            };

            sessionFactory = SessionFactoryHelper.Build(types);

            persisterForTestSaga = sessionFactory.GetEntityPersisterFor<TestSagaData>();
        }

        [Test]
        public void Id_generator_should_be_set_to_assigned()
        {
            Assert.That(typeof(Assigned), Is.EqualTo(persisterForTestSaga.IdentifierGenerator.GetType()));
        }

        [Test]
        public void Enums_should_be_mapped_as_integers()
        {
            persisterForTestSaga.ShouldContainMappingsFor<Status>();
        }

        [Test]
        public void Related_entities_should_also_be_mapped()
        {
            Assert.That(typeof(GuidCombGenerator), Is.EqualTo(sessionFactory.GetEntityPersisterFor<OrderLine>()
                                .IdentifierGenerator.GetType()));
        }

        [Test]
        public void Datetime_properties_should_be_mapped()
        {
            persisterForTestSaga.ShouldContainMappingsFor<DateTime>();
        }

        [Test]
        public void Public_setters_and_getters_of_concrete_classes_should_map_as_components()
        {
            persisterForTestSaga.ShouldContainMappingsFor<TestComponent>();
        }

        [Test]
        public void Users_can_override_autoMappings_by_embedding_hbm_files()
        {
            Assert.That(typeof(IdentityGenerator), Is.EqualTo(sessionFactory.GetEntityPersisterFor<TestSagaWithHbmlXmlOverride>()
                                .IdentifierGenerator.GetType()));
        }

        [Test]
        [Ignore("Not supported any more")]
        public void Inherited_property_classes_should_be_mapped()
        {
            persisterForTestSaga.ShouldContainMappingsFor<PolymorphicPropertyBase>();
            sessionFactory.ShouldContainPersisterFor<PolymorphicProperty>();
        }

        [Test]
        public void Users_can_override_tableNames_by_using_an_attribute()
        {
            var persister =
                sessionFactory.GetEntityPersister(typeof(SagaWithTableNameData).FullName).ClassMetadata as
                AbstractEntityPersister;
            Assert.That(persister.RootTableName, Is.EqualTo("MyTestSchema.MyTestTable"));
        }

        [Test]
        public void Users_can_override_tableNames_by_using_an_attribute_which_does_not_derive()
        {
            var persister =
                sessionFactory.GetEntityPersister(typeof(DerivedFromSagaWithTableNameData).FullName).
                    ClassMetadata as AbstractEntityPersister;
            Assert.That(persister.TableName, Is.EqualTo("DerivedFromSagaWithTableNameData"));
        }

        [Test]
        public void Users_can_override_derived_tableNames_by_using_an_attribute()
        {
            var persister =
                sessionFactory.GetEntityPersister(typeof(AlsoDerivedFromSagaWithTableNameData).FullName).
                    ClassMetadata as AbstractEntityPersister;
            Assert.That(persister.TableName, Is.EqualTo("MyDerivedTestTable"));
        }

        [Test]
        public void Array_of_ints_should_be_mapped_as_serializable()
        {
            var p = persisterForTestSaga.EntityMetamodel.Properties.SingleOrDefault(x => x.Name == "ArrayOfInts");
            Assert.IsNotNull(p);

            Assert.That(p.Type.GetType(), Is.EqualTo(global::NHibernate.NHibernateUtil.Serializable.GetType()));
        }

        [Test]
        public void Array_of_string_should_be_mapped_as_serializable()
        {
            var p = persisterForTestSaga.EntityMetamodel.Properties.SingleOrDefault(x => x.Name == "ArrayOfStrings");
            Assert.IsNotNull(p);

            Assert.That(p.Type.GetType(), Is.EqualTo(global::NHibernate.NHibernateUtil.Serializable.GetType()));
        }

        [Test]
        public void Array_of_dates_should_be_mapped_as_serializable()
        {
            var p = persisterForTestSaga.EntityMetamodel.Properties.SingleOrDefault(x => x.Name == "ArrayOfDates");
            Assert.IsNotNull(p);

            Assert.That(p.Type.GetType(), Is.EqualTo(global::NHibernate.NHibernateUtil.Serializable.GetType()));
        }

        [Test]
        public void Versioned_Property_should_override_optimistic_lock()
        {
            var persister1 = sessionFactory.GetEntityPersisterFor<SagaWithVersionedPropertyData>();
            var persister2 = sessionFactory.GetEntityPersisterFor<SagaWithoutVersionedPropertyData>();

            Assert.That(persister1.IsVersioned, Is.True);
            Assert.That(persister1.EntityMetamodel.IsDynamicUpdate, Is.False);
            Assert.That(persister1.EntityMetamodel.OptimisticLockMode, Is.EqualTo(Versioning.OptimisticLock.Version));

            Assert.That(persister2.EntityMetamodel.IsDynamicUpdate, Is.True);
            Assert.That(persister2.EntityMetamodel.OptimisticLockMode, Is.EqualTo(Versioning.OptimisticLock.All));
            Assert.That(persister2.IsVersioned, Is.False);
        }
    }

    public static class SessionFactoryExtensions
    {
        public static IEntityPersister GetEntityPersisterFor<T>(this SessionFactoryImpl sessionFactory)
        {
            return sessionFactory.GetEntityPersister(typeof(T).FullName);
        }

        public static void ShouldContainPersisterFor<T>(this SessionFactoryImpl sessionFactory)
        {
            Assert.NotNull(sessionFactory.GetEntityPersisterFor<T>());
        }
    }

    public static class EntityPersisterExtensions
    {
        public static void ShouldContainMappingsFor<T>(this IEntityPersister persister)
        {
            var result = persister.EntityMetamodel.Properties
                .Any(x => x.Type.ReturnedClass == typeof(T));

            Assert.That(result, Is.True);
        }
    }
}
