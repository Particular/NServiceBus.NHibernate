using NServiceBus;

namespace Test.NHibernate
{
    class OrderPlaced: IMessage
    {
        public long OrderId  { get; set; }
    }
}