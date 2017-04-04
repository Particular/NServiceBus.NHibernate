namespace NServiceBus.NHibernate.Tests.SynchronizedStorage
{
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;

    class TestEntity
    {
        public virtual string Id { get; set; }

        public class Mapping : ClassMapping<TestEntity>
        {
            public Mapping()
            {
                Id(x => x.Id, x => x.Generator(Generators.Assigned));
            }
        }
    }
}