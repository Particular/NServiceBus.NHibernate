namespace NServiceBus.NHibernate.Tests.Outbox;

using System;
using global::NHibernate;
using global::NHibernate.Mapping.ByCode;
using global::NHibernate.Mapping.ByCode.Conformist;
using NServiceBus.Outbox.NHibernate;

class MessageIdOutboxRecord : IOutboxRecord
{
    public virtual string MessageId { get; set; }
    public virtual bool Dispatched { get; set; }
    public virtual DateTime? DispatchedAt { get; set; }
    public virtual string TransportOperations { get; set; }
}

class MessageIdOutboxRecordMapping : ClassMapping<MessageIdOutboxRecord>
{
    public MessageIdOutboxRecordMapping()
    {
        Id(x => x.MessageId, m => m.Generator(Generators.Assigned));
        Property(p => p.Dispatched, pm =>
        {
            pm.Column(c => c.NotNullable(true));
        });
        Property(p => p.DispatchedAt);
        Property(p => p.TransportOperations, pm => pm.Type(NHibernateUtil.StringClob));
    }
}