namespace NServiceBus.Unicast.Subscriptions.NHibernate
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using global::NHibernate;
    using MessageDrivenSubscriptions;

    class CachedSubscriptionPersister : SubscriptionPersister
    {
        public CachedSubscriptionPersister(ISessionFactory sessionFactory, TimeSpan expiration)
            : base(sessionFactory)
        {
            this.expiration = expiration;
        }

        public override Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
        {
            cache.Clear();
            return base.Subscribe(subscriber, messageType, context, cancellationToken);
        }

        public override Task Unsubscribe(Subscriber address, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
        {
            cache.Clear();
            return base.Unsubscribe(address, messageType, context, cancellationToken);
        }

        public override async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context, CancellationToken cancellationToken = default)
        {
            var types = messageTypes.ToList();
            var typeNames = types.Select(mt => mt.TypeName).ToArray();
            var key = string.Join(",", typeNames);
            var cacheItemFound = cache.TryGetValue(key, out Tuple<DateTimeOffset, IEnumerable<Subscriber>> cacheItem);

            if (cacheItemFound && (DateTimeOffset.UtcNow - cacheItem.Item1) < expiration)
            {
                return cacheItem.Item2;
            }

            var baseSubscribers = await base.GetSubscriberAddressesForMessage(types, context, cancellationToken).ConfigureAwait(false);

            cacheItem = new Tuple<DateTimeOffset, IEnumerable<Subscriber>>(
                DateTimeOffset.UtcNow,
                baseSubscribers
                );

            cache.AddOrUpdate(key, s => cacheItem, (s, tuple) => cacheItem);

            return cacheItem.Item2;
        }

        ConcurrentDictionary<string, Tuple<DateTimeOffset, IEnumerable<Subscriber>>> cache = new ConcurrentDictionary<string, Tuple<DateTimeOffset, IEnumerable<Subscriber>>>();
        TimeSpan expiration;
    }
}