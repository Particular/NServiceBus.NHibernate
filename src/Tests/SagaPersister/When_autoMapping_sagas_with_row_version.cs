namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
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
        [RowVersion]
        public virtual byte[] MyVersion { get; set; }
    }

    public class MyDerivedClassWithRowVersionSaga : Saga<MyDerivedClassWithRowVersion>, IAmStartedByMessages<IMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyDerivedClassWithRowVersion> mapper)
        {
            mapper.ConfigureMapping<IMessage>(m => m.GetHashCode()).ToSaga(s => s.Id);
        }

        public Task Handle(IMessage message, IMessageHandlerContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}