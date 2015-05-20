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
            Property(p => p.MessageId, pm => pm.Column(c =>
            {
                c.NotNullable(true);
                c.Unique(true);
            }));
            Property(p => p.Dispatched, pm =>
            {
                pm.Column(c =>
                {
                    c.Default(0);
                    c.NotNullable(true);
                });
                pm.Index("OutboxRecord_Dispatched_Idx");
            });
            Property(p => p.DispatchedAt, pm => pm.Index("OutboxRecord_DispatchedAt_Idx"));
            Property(p => p.TransportOperations, pm => pm.Type(NHibernateUtil.StringClob));
        }
    }
}
