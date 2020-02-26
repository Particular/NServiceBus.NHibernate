namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Linq;
    using global::NHibernate.Cfg;
    using global::NHibernate.Engine;
    using global::NHibernate.Id;
    using global::NHibernate.Impl;
    using global::NHibernate.Persister.Entity;
    using Features;
    using global::NHibernate.Dialect;
    using NServiceBus.NHibernate.Tests;
    using Sagas;
    using Settings;
    using NUnit.Framework;

    [TestFixture]
    public class When_autoMapping_sagas
    {
        IEntityPersister persisterForTestSaga;
        SessionFactoryImpl sessionFactory;

        [OneTimeSetUp]
        public void SetUp()
        {
            var builder = new NHibernateSagaStorage();

            var cfg = new Configuration()
                .DataBaseIntegration(x =>
                {
                    x.Dialect<MsSql2012Dialect>();
                    x.ConnectionString = Consts.SqlConnectionString;
                });

            var settings = new SettingsHolder();

            var metaModel = new SagaMetadataCollection();
            var types = new [] {typeof(TestSaga), typeof(TestSagaData), typeof(TestComponent), typeof(PolymorphicPropertyBase),
                typeof(AlsoDerivedFromTestSagaWithTableNameAttributeActualSaga), typeof(AlsoDerivedFromTestSagaWithTableNameAttribute),
                typeof(DerivedFromTestSagaWithTableNameAttributeActualSaga), typeof(DerivedFromTestSagaWithTableNameAttribute),
                typeof(TestSagaWithTableNameAttributeActualSaga), typeof(TestSagaWithTableNameAttribute),
                typeof(SagaWithVersionedPropertyAttributeActualSaga), typeof(SagaWithVersionedPropertyAttribute),
                typeof(SagaWithoutVersionedPropertyAttributeActualSaga), typeof(SagaWithoutVersionedPropertyAttribute),
                typeof(object)
            };
            metaModel.Initialize(types);
            settings.Set(metaModel);

            settings.Set("TypesToScan", types);
            builder.ApplyMappings(settings, cfg);
            sessionFactory = cfg.BuildSessionFactory() as SessionFactoryImpl;

            persisterForTestSaga = sessionFactory.GetEntityPersisterFor<TestSagaData>();
        }

        [Test]
        public void Id_generator_should_be_set_to_assigned()
        {
            Assert.AreEqual(persisterForTestSaga.IdentifierGenerator.GetType(), typeof (Assigned));
        }

        [Test]
        public void Enums_should_be_mapped_as_integers()
        {
            persisterForTestSaga.ShouldContainMappingsFor<StatusEnum>();
        }

        [Test]
        public void Related_entities_should_also_be_mapped()
        {
            Assert.AreEqual(sessionFactory.GetEntityPersisterFor<OrderLine>()
                                .IdentifierGenerator.GetType(), typeof (GuidCombGenerator));
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
            Assert.AreEqual(sessionFactory.GetEntityPersisterFor<TestSagaWithHbmlXmlOverride>()
                                .IdentifierGenerator.GetType(), typeof (IdentityGenerator));
        }

        [Test,Ignore("Not supported any more")]
        public void Inherited_property_classes_should_be_mapped()
        {
            persisterForTestSaga.ShouldContainMappingsFor<PolymorphicPropertyBase>();
            sessionFactory.ShouldContainPersisterFor<PolymorphicProperty>();
        }

        [Test]
        public void Users_can_override_tableNames_by_using_an_attribute()
        {
            var persister =
                sessionFactory.GetEntityPersister(typeof (TestSagaWithTableNameAttribute).FullName).ClassMetadata as
                AbstractEntityPersister;
            Assert.AreEqual(persister.RootTableName, "MyTestSchema.MyTestTable");
        }

        [Test]
        public void Users_can_override_tableNames_by_using_an_attribute_which_does_not_derive()
        {
            var persister =
                sessionFactory.GetEntityPersister(typeof (DerivedFromTestSagaWithTableNameAttribute).FullName).
                    ClassMetadata as AbstractEntityPersister;
            Assert.AreEqual(persister.TableName, "DerivedFromTestSagaWithTableNameAttribute");
        }

        [Test]
        public void Users_can_override_derived_tableNames_by_using_an_attribute()
        {
            var persister =
                sessionFactory.GetEntityPersister(typeof (AlsoDerivedFromTestSagaWithTableNameAttribute).FullName).
                    ClassMetadata as AbstractEntityPersister;
            Assert.AreEqual(persister.TableName, "MyDerivedTestTable");
        }

        [Test]
        public void Array_of_ints_should_be_mapped_as_serializable()
        {
            var p = persisterForTestSaga.EntityMetamodel.Properties.SingleOrDefault(x => x.Name == "ArrayOfInts");
            Assert.IsNotNull(p);

            Assert.AreEqual(global::NHibernate.NHibernateUtil.Serializable.GetType(), p.Type.GetType());
        }

        [Test]
        public void Array_of_string_should_be_mapped_as_serializable()
        {
            var p = persisterForTestSaga.EntityMetamodel.Properties.SingleOrDefault(x => x.Name == "ArrayOfStrings");
            Assert.IsNotNull(p);

            Assert.AreEqual(global::NHibernate.NHibernateUtil.Serializable.GetType(), p.Type.GetType());
        }

        [Test]
        public void Array_of_dates_should_be_mapped_as_serializable()
        {
            var p = persisterForTestSaga.EntityMetamodel.Properties.SingleOrDefault(x => x.Name == "ArrayOfDates");
            Assert.IsNotNull(p);

            Assert.AreEqual(global::NHibernate.NHibernateUtil.Serializable.GetType(), p.Type.GetType());
        }

        [Test]
        public void Versioned_Property_should_override_optimistic_lock()
        {
          var persister1 = sessionFactory.GetEntityPersisterFor<SagaWithVersionedPropertyAttribute>();
          var persister2 = sessionFactory.GetEntityPersisterFor<SagaWithoutVersionedPropertyAttribute>();

          Assert.True(persister1.IsVersioned);
          Assert.False(persister1.EntityMetamodel.IsDynamicUpdate);
          Assert.AreEqual(Versioning.OptimisticLock.Version, persister1.EntityMetamodel.OptimisticLockMode);

          Assert.True(persister2.EntityMetamodel.IsDynamicUpdate);
          Assert.AreEqual(Versioning.OptimisticLock.All, persister2.EntityMetamodel.OptimisticLockMode);
          Assert.False(persister2.IsVersioned);
        }
    }

    public static class SessionFactoryExtensions
    {
        public static IEntityPersister GetEntityPersisterFor<T>(this SessionFactoryImpl sessionFactory)
        {
            return sessionFactory.GetEntityPersister(typeof (T).FullName);
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
                .Any(x => x.Type.ReturnedClass == typeof (T));

            Assert.True(result);
        }
    }
}
