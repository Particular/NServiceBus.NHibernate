namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<IMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
        {
            mapper.ConfigureMapping<IMessage>(m => m.GetHashCode()).ToSaga(s => s.Id);
        }

        public Task Handle(IMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class TestSagaData : IContainSagaData
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual RelatedClass RelatedClass { get; set; }

        public virtual IList<OrderLine> OrderLines { get; set; }

        public virtual StatusEnum Status { get; set; }

        public virtual DateTime DateTimeProperty { get; set; }

        public virtual TestComponent TestComponent { get; set; }

        public virtual PolymorphicPropertyBase PolymorphicRelatedProperty { get; set; }

        public virtual int[] ArrayOfInts { get; set; }
        public virtual string[] ArrayOfStrings { get; set; }
        public virtual DateTime[] ArrayOfDates { get; set; }
    }

    public class PolymorphicProperty : PolymorphicPropertyBase
    {
        public virtual int SomeInt { get; set; }
    }

    public class PolymorphicPropertyBase
    {
        public virtual Guid Id { get; set; }
    }

    public enum StatusEnum
    {
        SomeStatus,
        AnotherStatus
    }

    public class TestComponent
    {
        public string Property { get; set; }
        public string AnotherProperty { get; set; }
    }

    public class OrderLine
    {
        public virtual Guid Id { get; set; }

        public virtual Guid ProductId { get; set; }

    }


    public class RelatedClass
    {
        public virtual Guid Id { get; set; }


        public virtual TestSagaData ParentSaga { get; set; }
    }

    public class TestSagaWithHbmlXmlOverride : IContainSagaData
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual string SomeProperty { get; set; }
    }

    [TableName("MyTestTable", Schema = "MyTestSchema" )]
    public class TestSagaWithTableNameAttribute : IContainSagaData
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual string SomeProperty { get; set; }
    }

    public class TestSagaWithTableNameAttributeActualSaga : Saga<TestSagaWithTableNameAttribute>, IAmStartedByMessages<IMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaWithTableNameAttribute> mapper)
        {
            mapper.ConfigureMapping<IMessage>(m => m.GetHashCode()).ToSaga(s => s.Id);
        }

        public Task Handle(IMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class DerivedFromTestSagaWithTableNameAttribute : TestSagaWithTableNameAttribute
    { }

    public class DerivedFromTestSagaWithTableNameAttributeActualSaga : Saga<DerivedFromTestSagaWithTableNameAttribute>, IAmStartedByMessages<IMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DerivedFromTestSagaWithTableNameAttribute> mapper)
        {
            mapper.ConfigureMapping<IMessage>(m => m.GetHashCode()).ToSaga(s => s.Id);
        }

        public Task Handle(IMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }

    [TableName("MyDerivedTestTable")]
    public class AlsoDerivedFromTestSagaWithTableNameAttribute : TestSagaWithTableNameAttribute
    { }

    public class AlsoDerivedFromTestSagaWithTableNameAttributeActualSaga : Saga<AlsoDerivedFromTestSagaWithTableNameAttribute>, IAmStartedByMessages<IMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AlsoDerivedFromTestSagaWithTableNameAttribute> mapper)
        {
            mapper.ConfigureMapping<IMessage>(m => m.GetHashCode()).ToSaga(s => s.Id);
        }

        public Task Handle(IMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class SagaWithVersionedPropertyAttribute : IContainSagaData
    {
      public virtual Guid Id { get; set; }
      public virtual string Originator { get; set; }
      public virtual string OriginalMessageId { get; set; }

      [RowVersion]
      public virtual int Version { get; set; }
    }

    public class SagaWithoutVersionedPropertyAttribute : IContainSagaData
    {
      public virtual Guid Id { get; set; }
      public virtual string Originator { get; set; }
      public virtual string OriginalMessageId { get; set; }

      public virtual int Version { get; set; }
    }

    public class SagaWithVersionedPropertyAttributeActualSaga : Saga<SagaWithVersionedPropertyAttribute>, IAmStartedByMessages<IMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithVersionedPropertyAttribute> mapper)
        {
            mapper.ConfigureMapping<IMessage>(m => m.GetHashCode()).ToSaga(s => s.Id);
        }

        public Task Handle(IMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class SagaWithoutVersionedPropertyAttributeActualSaga : Saga<SagaWithoutVersionedPropertyAttribute>, IAmStartedByMessages<IMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithoutVersionedPropertyAttribute> mapper)
        {
            mapper.ConfigureMapping<IMessage>(m => m.GetHashCode()).ToSaga(s => s.Id);
        }

        public Task Handle(IMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }
}