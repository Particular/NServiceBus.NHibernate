namespace NServiceBus.TransactionalSession.AcceptanceTests.Infrastructure
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Features;
    using NServiceBus;
    using ObjectBuilder;

    public class CaptureBuilderFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var scenarioContext = context.Settings.Get<ScenarioContext>();
            context.RegisterStartupTask(builder => new CaptureServiceProviderStartupTask(builder, scenarioContext));
        }

        class CaptureServiceProviderStartupTask : FeatureStartupTask
        {
            public CaptureServiceProviderStartupTask(IBuilder builder, ScenarioContext context)
            {
                if (context is IInjectBuilder c)
                {
                    c.Builder = builder;
                }
            }

            protected override Task OnStart(IMessageSession session) => Task.CompletedTask;

            protected override Task OnStop(IMessageSession session) => Task.CompletedTask;
        }
    }
}