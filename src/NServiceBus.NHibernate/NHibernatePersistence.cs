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

                behaviorList.InnerList.Insert(0,typeof(UnitOfWorkBehavior));
                behaviorList.InnerList.Insert(1, typeof(NativeTransactionBehavior));
            }
        }
    }
}