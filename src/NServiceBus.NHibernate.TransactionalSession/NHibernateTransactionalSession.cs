namespace NServiceBus.TransactionalSession
{
    using Features;

    sealed class NHibernateTransactionalSession : TransactionalSession
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var endpointName = context.Settings.EndpointName();

            context.Container.ConfigureComponent<IOpenSessionOptionsCustomization>(b => new NHibernateTransactionalSessionOptionsCustomization(endpointName), DependencyLifecycle.SingleInstance);

            base.Setup(context);
        }
    }
}