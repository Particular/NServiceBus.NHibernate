namespace NServiceBus.Outbox.NHibernate
{
    using System;
    using System.Collections.Generic;

    class OutboxRecord
    {
        public virtual long Id { get; set; }
        public virtual string MessageId { get; set; }
        public virtual bool Dispatched { get; set; }
        public virtual DateTime? DispatchedAt { get; set; }
        public virtual IList<OutboxOperation> TransportOperations { get; set; }
    }

    class OutboxOperation
    {
        public virtual long Id { get; set; }
        public virtual string MessageId { get; set; }
        public virtual byte[] Message { get; set; }
        public virtual string Headers { get; set; }
        public virtual string Options { get; set; }
    }
}