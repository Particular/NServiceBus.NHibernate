namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;

    public class MySaga : Saga<MySagaData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
        {

        }
    }

    public class MySagaData : IContainSagaData
    {
        public virtual Guid Id { get; set; }
        public virtual string OriginalMessageId { get; set; }
        public virtual string Originator { get; set; }
    }
}
