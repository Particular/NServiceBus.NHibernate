namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System;
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;
    using global::NHibernate.Type;
    using NServiceBus.Outbox.NHibernate;

    class GuidOutboxRecord : IOutboxRecord
    {
        public virtual Guid Id { get; set; }
        public virtual string MessageId { get; set; }
        public virtual bool Dispatched { get; set; }
        public virtual DateTime? DispatchedAt { get; set; }
        public virtual string TransportOperations { get; set; }
    }

    class GuidOutboxRecordMapping : ClassMapping<GuidOutboxRecord>
    {
        public GuidOutboxRecordMapping()
        {
            Id(x => x.Id, m => m.Generator(Generators.Guid));
            Property(p => p.MessageId, pm => pm.Column(c =>
            {
                c.NotNullable(true);
                c.Unique(true);
            }));
            Property(p => p.Dispatched, pm =>
            {
                pm.Column(c => c.NotNullable(true));
            });
            Property(p => p.DispatchedAt, m => m.Type<DateTimeType>());
            Property(p => p.TransportOperations, pm => pm.Type(NHibernateUtil.StringClob));
        }
    }
}