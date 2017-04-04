namespace NServiceBus.TimeoutPersisters.NHibernate.Config
{
    using System;
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;
    using Persistence.NHibernate;

    /// <summary>
    /// Timeout entity map class
    /// </summary>
    class TimeoutEntityMap : ClassMapping<TimeoutEntity>
    {
        public const string EndpointIndexName = "TimeoutEntity_EndpointIdx";

        /// <summary>
        /// Default constructor
        /// </summary>
        public TimeoutEntityMap()
        {
            Id(x => x.Id, m => m.Generator(Generators.Assigned));
            Property(p => p.State, pm =>
            {
                pm.Type(NHibernateUtil.BinaryBlob);
                pm.Length(Int32.MaxValue);
            });
            Property(p => p.Destination, pm =>
            {
                pm.Type<AddressUserType>();
                pm.Length(1024);
            });
            Property(p => p.SagaId, pm => pm.Index("TimeoutEntity_SagaIdIdx"));
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
