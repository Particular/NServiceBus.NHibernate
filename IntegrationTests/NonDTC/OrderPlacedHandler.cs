using System;

namespace Test.NHibernate
{
    class OrderPlacedHandler : HandlerWithNHibernateSession<OrderPlaced>
    {
        public override void Handle(OrderPlaced message)
        {
            Console.Out.WriteLine("Order #{0} being shipped now", message.OrderId);

            var order = Session.Get<Entities.Order>(message.OrderId);

            order.Shipped = true;

            Session.Update(order);
        }
    }
}