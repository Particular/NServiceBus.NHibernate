namespace NServiceBus.Outbox.NHibernate
{
    using System.Threading.Tasks;
    using Extensibility;

    interface INHibernateOutboxTransaction : OutboxTransaction
    {
        Task Open<TEntity>(string endpointQualifiedMessageId)
            where TEntity : class, IOutboxRecord, new();
        Task Complete<TEntity>(string endpointQualifiedMessageId, OutboxMessage outboxMessage, ContextBag context)
            where TEntity : class, IOutboxRecord, new();
    }
}