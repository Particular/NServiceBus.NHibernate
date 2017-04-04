namespace NServiceBus.TimeoutPersisters.NHibernate.Config
{
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;

    class TimeoutEntityMap : ClassMapping<TimeoutEntity>
    {
        public const string EndpointIndexName = "TimeoutEntity_EndpointIdx";

        public TimeoutEntityMap()
        {
            Id(x => x.Id, m => m.Generator(Generators.GuidComb));
            Property(p => p.Destination, pm =>
            {
                pm.Length(1024);
            });
            Property(p => p.SagaId, pm => pm.Index("TimeoutEntity_SagaIdIdx"));
            Property(p => p.State, pm =>
            {
                pm.Type(NHibernateUtil.BinaryBlob);
                pm.Length(int.MaxValue);
            });
            Property(p => p.Endpoint, pm =>
            {
                pm.Index(EndpointIndexName);
                pm.Length(440);
            });
            Property(p => p.Time, pm => pm.Index(EndpointIndexName));
            Property(p => p.Headers, pm => pm.Type(NHibernateUtil.StringClob));
        }
    }

    
}
