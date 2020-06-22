namespace NServiceBus.Outbox.NHibernate
{
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using global::NHibernate;
    using Persistence.NHibernate;

    abstract class ConcurrencyControlStrategy
    {
        public abstract Task Begin(string endpointQualifiedMessageId, ISession session);
        public abstract Task Complete(string endpointQualifiedMessageId, ISession session, OutboxMessage outboxMessage, ContextBag context);
        protected static string ConvertOperations(OutboxMessage outboxMessage)
        {
            if (outboxMessage.TransportOperations.Length == 0)
            {
                return null;
            }
            var operations = outboxMessage.TransportOperations.Select(t => new OutboxOperation
            {
                Message = t.Body,
                Headers = t.Headers,
                MessageId = t.MessageId,
                Options = t.Options,
            });
            return ObjectSerializer.Serialize(operations);
        }
    }
}