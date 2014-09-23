using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Test.NHibernate.Entities
{
    class Order
    {
        public virtual long Id { get; set; }
        public virtual int Quantity { get; set; }
        public virtual string Product { get; set; }
        public virtual bool Shipped { get; set; }
    }

    class OrderMap : ClassMapping<Order>
    {
        public OrderMap()
        {
            Table("[Order]");
            Id(x => x.Id, m => m.Generator(Generators.Native));
            Property(p => p.Quantity);
            Property(p => p.Product);
            Property(p => p.Shipped);
        }
    }
}
