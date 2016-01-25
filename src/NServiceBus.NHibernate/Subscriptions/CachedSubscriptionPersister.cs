namespace NServiceBus.Unicast.Subscriptions.NHibernate
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::NHibernate;
    using NServiceBus.Extensibility;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    class CachedSubscriptionPersister : SubscriptionPersister
    {
        public CachedSubscriptionPersister(ISessionFactory sessionFactory, TimeSpan expiration) 
            : base(sessionFactory)
        {
            this.expiration = expiration;
        }

        public override Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            base.Subscribe(subscriber, messageType, context);
            cache.Clear();
            return Task.FromResult(0);
        }

        public override Task Unsubscribe(Subscriber address, MessageType messageType, ContextBag context)
        {
            base.Unsubscribe(address, messageType, context);
            cache.Clear();
            return Task.FromResult(0);
        }

        public override async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var types = messageTypes.ToList();
            var typeNames = types.Select(mt => mt.TypeName).ToArray();
            var key = string.Join(",", typeNames);
            Tuple<DateTimeOffset, IEnumerable<Subscriber>> cacheItem;
            var cacheItemFound = cache.TryGetValue(key, out cacheItem);

            if (cacheItemFound && (DateTimeOffset.UtcNow - cacheItem.Item1) < expiration)
            {
                return cacheItem.Item2;
            }

            var baseSubscribers = await base.GetSubscriberAddressesForMessage(types, context);

            cacheItem = new Tuple<DateTimeOffset, IEnumerable<Subscriber>>(
                DateTimeOffset.UtcNow,
                baseSubscribers
                );

            cache.AddOrUpdate(key, s => cacheItem, (s, tuple) => cacheItem);

            return cacheItem.Item2;
        }

        static ConcurrentDictionary<string, Tuple<DateTimeOffset, IEnumerable<Subscriber>>> cache = new ConcurrentDictionary<string, Tuple<DateTimeOffset, IEnumerable<Subscriber>>>();
        TimeSpan expiration;
    }
}