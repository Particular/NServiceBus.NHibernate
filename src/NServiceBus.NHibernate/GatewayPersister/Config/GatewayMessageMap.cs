namespace NServiceBus.GatewayPersister.NHibernate.Config
{
    using System;
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;

    /// <summary>
    /// Gateway message mapping class.
    /// </summary>
    public class GatewayMessageMap : ClassMapping<GatewayMessage>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public GatewayMessageMap()
        {
            Table("GatewayMessages");
            Id(x => x.Id, m => m.Generator(Generators.Assigned));
            Property(p => p.OriginalMessage, pm =>
            {
                pm.Type(NHibernateUtil.BinaryBlob);
                pm.Length(Int32.MaxValue);
            });
            Property(p => p.Acknowledged);
            Property(p => p.TimeReceived);
            Property(p => p.Headers, pm => pm.Type(NHibernateUtil.StringClob));
        }
    }
}