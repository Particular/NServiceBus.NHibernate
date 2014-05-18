namespace NServiceBus.Outbox.NHibernate
{
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;
    using NServiceBus.NHibernate.Internal;

    class OutboxEntityMap : ClassMapping<OutboxRecord>
    {
        public OutboxEntityMap()
        {
            Id(x => x.Id, m => m.Generator(Generators.Native));
            Property(p => p.MessageId, pm =>
            {
                pm.Index("MessageId_Index");
                pm.Column(c =>
                {
                    c.NotNullable(true);
                    c.Unique(true);
                });
            });
            Property(p => p.Dispatched, pm => pm.Column(c =>
            {
                c.Default(0);
                c.NotNullable(true);
            }));
            Property(p => p.DispatchedAt, pm => pm.Index("DispatchedAt_Index"));
            Bag(p=>p.TransportOperations, b =>
            {
                b.Cascade(Cascade.All | Cascade.DeleteOrphans);
                b.Key(km => km.Column("OutboxOperation_id"));
            }, r=> r.OneToMany());
        }
    }

    class TransportOperationEntityMap : ClassMapping<OutboxOperation>
    {
        public TransportOperationEntityMap()
        {
            Id(x => x.Id, m => m.Generator(Generators.Native));
            Property(p => p.Intent);
            Property(p => p.CorrelationId, pm => pm.Length(1024));
            Property(p => p.MessageId, pm => pm.Column(c => c.NotNullable(true)));
            Property(p => p.MessageType, pm => pm.Column(c => c.NotNullable(true)));
            Property(p => p.Destination, pm =>
            {
                pm.Type<AddressUserType>();
                pm.Length(1024);
            });
            Property(p => p.ReplyToAddress, pm =>
            {
                pm.Type<AddressUserType>();
                pm.Length(1024);
            });
            Property(p => p.DeliverAt);
            Property(p => p.DelayDeliveryWith);
            Property(p => p.EnforceMessagingBestPractices);
            Property(p => p.Message, pm => pm.Type(NHibernateUtil.BinaryBlob));
            Property(p => p.Headers, pm => pm.Type(NHibernateUtil.StringClob));
        }
    }
}
