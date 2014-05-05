namespace NServiceBus.Outbox.NHibernate
{
    using System;
    using System.Collections.Generic;
    using NServiceBus;

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
        public virtual MessageIntentEnum Intent { get; set; }
        public virtual Address Destination { get; set; }
        public virtual string CorrelationId { get; set; }
        public virtual Address ReplyToAddress { get; set; }
        public virtual DateTime? DeliverAt { get; set; }
        public virtual TimeSpan? DelayDeliveryWith { get; set; }
        public virtual bool EnforceMessagingBestPractices { get; set; }
        public virtual string MessageId { get; set; }
        public virtual byte[] Message { get; set; }
        public virtual string Headers { get; set; }
    }
}