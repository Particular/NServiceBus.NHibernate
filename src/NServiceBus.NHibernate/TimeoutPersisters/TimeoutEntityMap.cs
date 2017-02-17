namespace NServiceBus.TimeoutPersisters.NHibernate.Config
{
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;

    class TimeoutEntityMap : ClassMapping<TimeoutEntity>
    {
        public TimeoutEntityMap()
        {
            Id(x => x.Id, m => m.Generator(Generators.GuidComb));
            Property(p => p.State, pm =>
            {
                pm.Type(NHibernateUtil.BinaryBlob);
                pm.Length(int.MaxValue);
            });
            Property(p => p.Destination, pm =>
            {
                pm.Length(1024);
            });
            Property(p => p.SagaId, pm => pm.Index("TimeoutEntity_SagaIdIdx"));
            Property(p => p.Time, pm => pm.Index("TimeoutEntity_EndpointIdx"));
            Property(p => p.Headers, pm => pm.Type(NHibernateUtil.StringClob));
            Property(p => p.Endpoint, pm =>
            {
                pm.Index("TimeoutEntity_EndpointIdx");
                pm.Length(440);
            });
        }
    }

    
}
