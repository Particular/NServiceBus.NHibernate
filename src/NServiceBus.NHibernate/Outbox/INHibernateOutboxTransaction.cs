namespace NServiceBus.Outbox.NHibernate
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using global::NHibernate;

    interface INHibernateOutboxTransaction : OutboxTransaction
    {
        ISession Session { get; }
        void OnSaveChanges(Func<Task> callback);
        void Prepare();
        Task<OutboxTransaction> Begin(string endpointQualifiedMessageId);
        Task Complete(string endpointQualifiedMessageId, OutboxMessage outboxMessage, ContextBag context);
        void BeginSynchronizedSession(ContextBag context);
    }
}