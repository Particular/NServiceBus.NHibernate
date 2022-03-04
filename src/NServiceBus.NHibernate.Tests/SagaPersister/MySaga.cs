namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;

    public class MySaga : Saga<MySagaData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
        {

        }
    }

    public class MySagaData : ContainSagaData
    {
        public Guid SagaId { get; set; }
    }
}
