using NHibernate;
using NServiceBus.Pipeline;
using NServiceBus.Saga;

namespace Test.NHibernate
{
    abstract class SagaWithNHibernateSession<T> : Saga<T> where T : IContainSagaData, new()
    {
        public PipelineExecutor PipelineExecutor { get; set; }

        public ISession Session
        {
            get { return PipelineExecutor.CurrentContext.Get<ISession>(); }
        }
    }
}