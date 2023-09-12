namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<SagaStartMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
        {
            mapper.ConfigureMapping<SagaStartMessage>(m => m.CorrelationId).ToSaga(s => s.SagaCorrelationId);
        }

        public Task Handle(SagaStartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class TestSagaData : ContainSagaData
    {
        public virtual Guid SagaCorrelationId { get; set; }
        public virtual RelatedClass RelatedClass { get; set; }

        public virtual IList<OrderLine> OrderLines { get; set; }

        public virtual Status Status { get; set; }

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

    public enum Status
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

    public class TestSagaWithHbmlXmlOverride : ContainSagaData
    {
        public virtual string SomeProperty { get; set; }
    }

    [TableName("MyTestTable", Schema = "MyTestSchema")]
    public class SagaWithTableNameData : ContainSagaData
    {
        public virtual Guid CorrelationId { get; set; }
        public virtual string SomeProperty { get; set; }
    }

    public class SagaStartMessage : IMessage
    {
        public virtual Guid CorrelationId { get; set; }
    }

    public class SagaWithTableName : Saga<SagaWithTableNameData>, IAmStartedByMessages<SagaStartMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithTableNameData> mapper)
        {
            mapper.ConfigureMapping<SagaStartMessage>(m => m.CorrelationId).ToSaga(s => s.CorrelationId);
        }

        public Task Handle(SagaStartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class DerivedFromSagaWithTableNameData : SagaWithTableNameData
    { }

    public class DerivedFromSagaWithTableName : Saga<DerivedFromSagaWithTableNameData>, IAmStartedByMessages<SagaStartMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DerivedFromSagaWithTableNameData> mapper)
        {
            mapper.ConfigureMapping<SagaStartMessage>(m => m.CorrelationId).ToSaga(s => s.CorrelationId);
        }

        public Task Handle(SagaStartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }

    [TableName("MyDerivedTestTable")]
    public class AlsoDerivedFromSagaWithTableNameData : SagaWithTableNameData
    { }

    public class AlsoDerivedFromSagaWithTableName : Saga<AlsoDerivedFromSagaWithTableNameData>, IAmStartedByMessages<SagaStartMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AlsoDerivedFromSagaWithTableNameData> mapper)
        {
            mapper.ConfigureMapping<SagaStartMessage>(m => m.CorrelationId).ToSaga(s => s.CorrelationId);
        }

        public Task Handle(SagaStartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class SagaWithVersionedPropertyData : IContainSagaData
    {
        [RowVersion]
        public virtual int Version { get; set; }
        public virtual Guid CorrelationId { get; set; }
        public virtual Guid Id { get; set; }
        public virtual string Originator { get; set; }
        public virtual string OriginalMessageId { get; set; }
    }

    public class SagaWithoutVersionedPropertyData : ContainSagaData
    {
        public virtual Guid CorrelationId { get; set; }
        public virtual int Version { get; set; }
    }

    public class SagaWithVersionedProperty : Saga<SagaWithVersionedPropertyData>, IAmStartedByMessages<SagaStartMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithVersionedPropertyData> mapper)
        {
            mapper.ConfigureMapping<SagaStartMessage>(m => m.CorrelationId).ToSaga(s => s.CorrelationId);
        }

        public Task Handle(SagaStartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class SagaWithoutVersionedProperty : Saga<SagaWithoutVersionedPropertyData>, IAmStartedByMessages<SagaStartMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithoutVersionedPropertyData> mapper)
        {
            mapper.ConfigureMapping<SagaStartMessage>(m => m.CorrelationId).ToSaga(s => s.CorrelationId);
        }

        public Task Handle(SagaStartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }
}