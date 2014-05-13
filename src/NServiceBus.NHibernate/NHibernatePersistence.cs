namespace NServiceBus.UnitOfWork.NHibernate
{
    using Features;
    using Pipeline;
    using Pipeline.Contexts;

    class NHibernatePersistence:Feature
    {
        public override bool IsEnabledByDefault
        {
            get { return true; }
        }


        class PipelineConfig:PipelineOverride
        {
            public override void Override(BehaviorList<ReceivePhysicalMessageContext> behaviorList)
            {
                if (!IsEnabled<NHibernatePersistence>())
                {
                    return;
                }

                //this one needs to go first to make sure that the outbox have a connection
                behaviorList.InnerList.Insert(0,typeof(OpenSqlConnectionBehavior));

                //we run after the units of work to allow the users to provide their own session
                behaviorList.InsertAfter<UnitOfWorkBehavior,OpenSessionBehavior>();

                //we open a native NH tx if needed right after the session has been created
                behaviorList.InsertAfter<OpenSessionBehavior, OpenNativeTransactionBehavior>();
            }
        }
    }
}