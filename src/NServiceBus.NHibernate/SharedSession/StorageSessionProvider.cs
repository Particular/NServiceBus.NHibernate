namespace NServiceBus.NHibernate.SharedSession
{
    using System;
    using global::NHibernate;
    using Pipeline;

    class StorageSessionProvider : IStorageSessionProvider
    {
        public PipelineExecutor PipelineExecutor { get; set; }

        public string ConnectionString { get; set; }

        public ISession Session
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
    }
}