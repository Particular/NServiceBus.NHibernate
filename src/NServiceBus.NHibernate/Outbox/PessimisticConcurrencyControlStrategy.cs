namespace NServiceBus.Outbox.NHibernate
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using global::NHibernate;

    class PessimisticConcurrencyControlStrategy<TEntity> : ConcurrencyControlStrategy where TEntity : class, IOutboxRecord, new()
    {
        public override Task Begin(string endpointQualifiedMessageId, ISession session, CancellationToken cancellationToken = default)
        {
            var outboxRecord = new TEntity
            {
                MessageId = endpointQualifiedMessageId,
                Dispatched = false,
                TransportOperations = null
            };
            return session.SaveAsync(outboxRecord, cancellationToken);
        }

        public override Task Complete(string endpointQualifiedMessageId, ISession session, OutboxMessage outboxMessage, ContextBag context, CancellationToken cancellationToken = default)
        {
            var queryString = $"update {typeof(TEntity).Name} set TransportOperations = :ops where MessageId = :messageid";
            return session.CreateQuery(queryString)
                .SetString("messageid", endpointQualifiedMessageId)
                .SetString("ops", ConvertOperations(outboxMessage))
                .ExecuteUpdateAsync(cancellationToken);
        }
    }
}