namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using AcceptanceTesting;
    using CriticalError = NServiceBus.CriticalError;

    static class SubscriptionBehaviorExtensions
    {
        public static void OnEndpointSubscribed<TContext>(this EndpointConfiguration b, Action<SubscriptionEventArgs, TContext> action) where TContext : ScenarioContext
        {
            b.Pipeline.Register<SubscriptionBehavior<TContext>.Registration>();

            b.RegisterComponents(c => c.ConfigureComponent(builder =>
            {
                var context = builder.Build<TContext>();
                return new SubscriptionBehavior<TContext>(action, context, builder.Build<CriticalError>(), MessageIntentEnum.Subscribe);
            }, DependencyLifecycle.InstancePerCall));
        }

        public static void OnEndpointUnsubscribed<TContext>(this EndpointConfiguration b, Action<SubscriptionEventArgs, TContext> action) where TContext : ScenarioContext
        {
            b.Pipeline.Register<SubscriptionBehavior<TContext>.Registration>();

            b.RegisterComponents(c => c.ConfigureComponent(builder =>
            {
                var context = builder.Build<TContext>();
                return new SubscriptionBehavior<TContext>(action, context, builder.Build<CriticalError>(), MessageIntentEnum.Unsubscribe);
            }, DependencyLifecycle.InstancePerCall));
        }
    }
}