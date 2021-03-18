namespace NServiceBus.Outbox.NHibernate
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using global::NHibernate;

    interface INHibernateOutboxTransaction : OutboxTransaction
    {
        ISession Session { get; }
        void OnSaveChanges(Func<CancellationToken, Task> callback);
        // Prepare is deliberately kept sync to allow floating of TxScope where needed
        void Prepare();
        Task Begin(string endpointQualifiedMessageId, CancellationToken cancellationToken = default);
        Task Complete(string endpointQualifiedMessageId, OutboxMessage outboxMessage, ContextBag context, CancellationToken cancellationToken = default);
        void BeginSynchronizedSession(ContextBag context);
    }
}