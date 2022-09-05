namespace NServiceBus.TransactionalSession;

using Features;
using Microsoft.Extensions.DependencyInjection;

sealed class NHibernateTransactionalSession : Feature
{
    public NHibernateTransactionalSession()
    {
        Defaults(s =>
        {
            s.EnableFeatureByDefault<TransactionalSession>();
        });

        DependsOn<SynchronizedStorage>();
        DependsOn<TransactionalSession>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var endpointName = context.Settings.EndpointName();

        context.Services.AddSingleton(typeof(IOpenSessionOptionsCustomization), new NHibernateTransactionalSessionOptionsCustomization(endpointName));
    }
}