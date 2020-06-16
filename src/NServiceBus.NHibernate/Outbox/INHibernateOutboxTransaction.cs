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
        Task Begin(string endpointQualifiedMessageId);
        Task Complete(string endpointQualifiedMessageId, OutboxMessage outboxMessage, ContextBag context);
    }
}