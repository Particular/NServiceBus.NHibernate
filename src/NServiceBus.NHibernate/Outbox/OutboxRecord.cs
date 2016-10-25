namespace NServiceBus.Outbox.NHibernate
{
    using System;

    class OutboxRecord : IOutboxRecord
    {
        public virtual long Id { get; set; }
        public virtual string MessageId { get; set; }
        public virtual bool Dispatched { get; set; }
        public virtual DateTime? DispatchedAt { get; set; }
        public virtual string TransportOperations { get; set; }
    }
}