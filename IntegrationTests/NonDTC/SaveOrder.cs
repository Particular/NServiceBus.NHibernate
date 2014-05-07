using System;
using NServiceBus.Saga;

namespace Test.NHibernate
{
    class SaveOrder : SagaWithNHibernateSession<SaveOrder.OrderData>, IAmStartedByMessages<NewOrder>, IHandleTimeouts<BuyersRemorseIsOver>
    {
        public void Handle(NewOrder message)
        {
            Console.Out.WriteLine("Processing order");

            RequestTimeout(TimeSpan.FromSeconds(5), new BuyersRemorseIsOver());

            Data.Product = message.Product;
            Data.Quantity = message.Quantity;
        }

        public void Timeout(BuyersRemorseIsOver state)
        {
            Console.Out.WriteLine("Order fulfilled");

            var order = new Entities.Order
            {
                Product = Data.Product,
                Quantity = Data.Quantity
            };

            Session.Save(order);

            Bus.Reply(new OrderPlaced
            {
                OrderId = order.Id,
            });

            MarkAsComplete();
        }

        internal class OrderData : ContainSagaData
        {
            public virtual int Quantity { get; set; }
            public virtual string Product { get; set; }
        }
    }

    class BuyersRemorseIsOver
    {
    }
}