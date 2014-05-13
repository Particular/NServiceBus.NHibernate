namespace NServiceBus.UnitOfWork.NHibernate
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
                ISession existingSession;

                if (!PipelineExecutor.CurrentContext.TryGet(string.Format("NHibernateSession-{0}", ConnectionString), out existingSession))
                {
                    throw new Exception("No active storage session found in context");
                }

                return existingSession;
            }
        }
    }
}