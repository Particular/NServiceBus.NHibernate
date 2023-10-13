namespace NServiceBus.Outbox.NHibernate
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using global::NHibernate;
    using Persistence.NHibernate;

    abstract class ConcurrencyControlStrategy
    {
        public abstract Task Begin(string endpointQualifiedMessageId, ISession session, CancellationToken cancellationToken = default);
        public abstract Task Complete(string endpointQualifiedMessageId, ISession session, OutboxMessage outboxMessage, ContextBag context, CancellationToken cancellationToken = default);
        protected static string ConvertOperations(OutboxMessage outboxMessage)
        {
            if (outboxMessage.TransportOperations.Length == 0)
            {
                return null;
            }
            var operations = outboxMessage.TransportOperations.Select(t => new OutboxOperation
            {
                Message = t.Body.ToArray(),
                Headers = t.Headers,
                MessageId = t.MessageId,
                Options = t.Options != null ? new Dictionary<string, string>(t.Options) : [],
            });
            return ObjectSerializer.Serialize(operations);
        }
    }
}