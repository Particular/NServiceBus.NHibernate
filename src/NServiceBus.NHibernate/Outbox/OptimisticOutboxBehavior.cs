namespace NServiceBus.Outbox.NHibernate
{
    using System.Threading.Tasks;
    using Extensibility;
    using global::NHibernate;

    class OptimisticOutboxBehavior<TEntity> : OutboxBehavior where TEntity : class, IOutboxRecord, new()
    {
        public override Task Begin(string endpointQualifiedMessageId, ISession session)
        {
            return Task.CompletedTask;
        }

        public override Task Complete(string endpointQualifiedMessageId, ISession session, OutboxMessage outboxMessage, ContextBag context)
        {
            var record = new TEntity
            {
                MessageId = endpointQualifiedMessageId,
                Dispatched = false,
                TransportOperations = ConvertOperations(outboxMessage)
            };
            return session.SaveAsync(record);
        }
    }
}