namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using Saga;

    public class MySaga : Saga<MySagaData>
    {

    }

    public class MySagaData : IContainSagaData
    {
        public virtual Guid Id { get; set; }
        public virtual string OriginalMessageId { get; set; }
        public virtual string Originator { get; set; }
    }
}
