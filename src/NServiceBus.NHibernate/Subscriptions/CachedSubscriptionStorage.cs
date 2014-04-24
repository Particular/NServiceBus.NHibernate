namespace NServiceBus.Unicast.Subscriptions.NHibernate
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    public class CachedSubscriptionStorage : SubscriptionStorage
    {
        TimeSpan _expiration;

        public CachedSubscriptionStorage(ISubscriptionStorageSessionProvider subscriptionStorageSessionProvider, TimeSpan expiration) : base(subscriptionStorageSessionProvider)
        {
            _expiration = expiration;
        }

        public override void Subscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            base.Subscribe(address, messageTypes);
            _cache.Clear();
        }

        public override void Unsubscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            base.Unsubscribe(address, messageTypes);
            _cache.Clear();
        }

        public override IEnumerable<Address> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            var types = messageTypes.ToList();
            
            var typeNames = types.Select(mt => mt.TypeName).ToArray();

            var key = String.Join(",", typeNames);

            Tuple<DateTimeOffset, IEnumerable<Address>> cacheItem;
            var cacheItemFound = _cache.TryGetValue(key, out cacheItem);
            if (cacheItemFound && (DateTimeOffset.UtcNow - cacheItem.Item1) < _expiration)
                return cacheItem.Item2;

            cacheItem = new Tuple<DateTimeOffset, IEnumerable<Address>>(
                DateTimeOffset.UtcNow,
                base.GetSubscriberAddressesForMessage(types)
                );

            _cache.AddOrUpdate(key, s => cacheItem, (s, tuple) => cacheItem);

            return cacheItem.Item2;
        }

        static readonly ConcurrentDictionary<string, Tuple<DateTimeOffset, IEnumerable<Address>>> _cache = new ConcurrentDictionary<string, Tuple<DateTimeOffset, IEnumerable<Address>>>();
    }
}