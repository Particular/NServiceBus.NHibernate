namespace NServiceBus.Outbox.NHibernate
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using global::NHibernate;

    class OptimisticConcurrencyControlStrategy<TEntity> : ConcurrencyControlStrategy where TEntity : class, IOutboxRecord, new()
    {
        public override Task Begin(string endpointQualifiedMessageId, ISession session, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public override Task Complete(string endpointQualifiedMessageId, ISession session, OutboxMessage outboxMessage, ContextBag context, CancellationToken cancellationToken = default)
        {
            var record = new TEntity
            {
                MessageId = endpointQualifiedMessageId,
                Dispatched = false,
                TransportOperations = ConvertOperations(outboxMessage)
            };
            return session.SaveAsync(record, cancellationToken);
        }
    }
}