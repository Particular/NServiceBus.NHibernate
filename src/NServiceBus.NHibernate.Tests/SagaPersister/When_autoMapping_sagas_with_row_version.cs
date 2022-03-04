namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate;
    using NUnit.Framework;

    [TestFixture]
    public class When_autoMapping_sagas_with_row_version
    {
        [Test]
        public void Should_throw_if_class_is_derived()
        {
            Assert.Throws<MappingException>(() =>
            {
                SessionFactoryHelper.Build(new[]
                {
                    typeof(MyDerivedClassWithRowVersionSaga),
                    typeof(MyDerivedClassWithRowVersion)
                });
            });
        }
    }

    public class MyDerivedClassWithRowVersion : ContainSagaData
    {
        public Guid SagaId { get; set; }
        [RowVersion]
        public virtual byte[] MyVersion { get; set; }
    }

    public class MyDerivedClassWithRowVersionSaga : Saga<MyDerivedClassWithRowVersion>, IAmStartedByMessages<SagaStartMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyDerivedClassWithRowVersion> mapper)
        {
            mapper.ConfigureMapping<SagaStartMessage>(m => m.SagaId).ToSaga(s => s.SagaId);
        }

        public Task Handle(SagaStartMessage message, IMessageHandlerContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}