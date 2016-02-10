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
        public virtual string TransportOperations { get; set; }
    }

    [Serializable]
    class OutboxOperation
    {
        public string MessageId { get; set; }
        public byte[] Message { get; set; }
        public Dictionary<string, string> Options { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}