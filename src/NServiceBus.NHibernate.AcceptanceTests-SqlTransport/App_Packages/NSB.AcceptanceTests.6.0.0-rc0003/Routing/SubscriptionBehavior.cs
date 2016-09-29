namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.Pipeline;
    using Transport;
    using CriticalError = NServiceBus.CriticalError;

    class SubscriptionBehavior<TContext> : Behavior<IIncomingPhysicalMessageContext> where TContext : ScenarioContext
    {
        public SubscriptionBehavior(Action<SubscriptionEventArgs, TContext> action, TContext scenarioContext, CriticalError criticalError, MessageIntentEnum intentToHandle)
        {
            this.action = action;
            this.scenarioContext = scenarioContext;
            this.criticalError = criticalError;
            this.intentToHandle = intentToHandle;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            const int maxRetries = 10;
            var retries = 0;
            var succeeded = false;
            Exception lastError = null;
            while (retries < maxRetries && !succeeded)
            {
                try
                {
                    await next().ConfigureAwait(false);
                    succeeded = true;
                }
                catch (Exception ex)
                {
                    retries++;
                    lastError = ex;
                    Thread.Sleep(100);
                }
            }
            if (!succeeded)
            {
                criticalError.Raise("Error updating subscription store", lastError);
            }
            var intent = (MessageIntentEnum)Enum.Parse(typeof(MessageIntentEnum), context.Message.Headers[Headers.MessageIntent], true);
            if (intent != intentToHandle)
            {
                return;
            }
            var subscriptionMessageType = GetSubscriptionMessageTypeFrom(context.Message);
            if (subscriptionMessageType != null)
            {
                string returnAddress;
                if (!context.Message.Headers.TryGetValue(Headers.SubscriberTransportAddress, out returnAddress))
                {
                    context.Message.Headers.TryGetValue(Headers.ReplyToAddress, out returnAddress);
                }
                action(new SubscriptionEventArgs
                {
                    MessageType = subscriptionMessageType,
                    SubscriberReturnAddress = returnAddress
                }, scenarioContext);
            }
        }

        static string GetSubscriptionMessageTypeFrom(IncomingMessage msg)
        {
            return (from header in msg.Headers where header.Key == Headers.SubscriptionMessageType select header.Value).FirstOrDefault();
        }

        Action<SubscriptionEventArgs, TContext> action;
        TContext scenarioContext;
        CriticalError criticalError;
        MessageIntentEnum intentToHandle;

        internal class Registration : RegisterStep
        {
            public Registration()
                : base("SubscriptionBehavior", typeof(SubscriptionBehavior<TContext>), "So we can get subscription events")
            {
                InsertBeforeIfExists("ProcessSubscriptionRequests");
            }
        }
    }
}