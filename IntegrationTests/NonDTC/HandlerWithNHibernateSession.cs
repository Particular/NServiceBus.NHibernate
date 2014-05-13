using NHibernate;
using NServiceBus;
using NServiceBus.Pipeline;

namespace Test.NHibernate
{
    abstract class HandlerWithNHibernateSession<T> : IHandleMessages<T>
    {
        public IBus Bus { get; set; }

        public ISession Session
        {
            get
            {
                return Configure.Instance.Builder.Build<PipelineExecutor>().CurrentContext.Get<ISession>();
            }
        }

        public abstract void Handle(T message);
    }
}