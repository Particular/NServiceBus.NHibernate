namespace NServiceBus.TransactionalSession
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    sealed class NHibernateTransactionalSession : TransactionalSession
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var endpointName = context.Settings.EndpointName();

            context.Services.AddSingleton(typeof(IOpenSessionOptionsCustomization), new NHibernateTransactionalSessionOptionsCustomization(endpointName));
        }
    }
}