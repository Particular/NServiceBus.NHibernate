using NServiceBus;

namespace Test.NHibernate
{
    class NewOrder: ICommand
    {
        public string Product { get; set; }
        public int Quantity { get; set; }
    }
}