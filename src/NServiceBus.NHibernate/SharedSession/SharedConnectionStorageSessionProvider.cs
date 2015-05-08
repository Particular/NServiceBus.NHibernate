namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using global::NHibernate;
    using Pipeline;

    class SharedConnectionStorageSessionProvider : IStorageSessionProvider
    {
        public PipelineExecutor PipelineExecutor { get; set; }

        public string ConnectionString { get; set; }

        ISession Session
        {
            get
            {
                Lazy<ISession> existingSession;

                if (!PipelineExecutor.CurrentContext.TryGet(string.Format("LazyNHibernateSession-{0}", ConnectionString), out existingSession))
                {
                    throw new Exception("No active storage session found in context");
                }

                return existingSession.Value;
            }
        }

        public void ExecuteInTransaction(Action<ISession> operation)
        {
            operation(Session);
        }
    }
}