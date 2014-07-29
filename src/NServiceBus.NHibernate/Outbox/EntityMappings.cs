namespace NServiceBus.Outbox.NHibernate
{
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;

    class OutboxEntityMap : ClassMapping<OutboxRecord>
    {
        public OutboxEntityMap()
        {
            Id(x => x.Id, m => m.Generator(Generators.Native));
            Property(p => p.MessageId, pm =>
            {
                pm.Index("OutboxRecord_MessageId_Index");
                pm.Column(c =>
                {
                    c.NotNullable(true);
                    c.Unique(true);
                });
            });
            Property(p => p.Dispatched, pm =>
            {
                pm.Column(c =>
                {
                    c.Default(0);
                    c.NotNullable(true);
                });
                pm.Index("OutboxRecord_Dispatched_Index");
            });
            Property(p => p.DispatchedAt, pm => pm.Index("OutboxRecord_DispatchedAt_Index"));
            Bag(p => p.TransportOperations, b =>
            {
                b.Cascade(Cascade.All | Cascade.DeleteOrphans);
                b.Key(km => km.Column("OutboxRecord_id"));
            }, r => r.OneToMany());
        }
    }

    class TransportOperationEntityMap : ClassMapping<OutboxOperation>
    {
        public TransportOperationEntityMap()
        {
            Id(x => x.Id, m => m.Generator(Generators.Native));
            Property(p => p.MessageId, pm =>
            {
                pm.Index("OutboxOperation_MessageId_Index");
                pm.Column(c => c.NotNullable(true));
            });
            Property(p => p.Message, pm => pm.Type(NHibernateUtil.BinaryBlob));
            Property(p => p.Headers, pm => pm.Type(NHibernateUtil.StringClob));
            Property(p => p.Options, pm => pm.Type(NHibernateUtil.StringClob));
        }
    }
}
