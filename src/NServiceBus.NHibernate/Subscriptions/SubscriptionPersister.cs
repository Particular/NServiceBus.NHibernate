namespace NServiceBus.Unicast.Subscriptions.NHibernate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using global::NHibernate;
    using global::NHibernate.Criterion;
    using Logging;
    using MessageDrivenSubscriptions;
    using IsolationLevel = System.Data.IsolationLevel;

    class SubscriptionPersister : ISubscriptionStorage
    {
        static ILog Logger = LogManager.GetLogger(typeof(ISubscriptionStorage));
        static IEqualityComparer<Subscription> SubscriptionComparer = new SubscriptionByTransportAddressComparer();
        ISessionFactory sessionFactory;

        public SubscriptionPersister(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public virtual Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
        {
            return Retry(ct => SubscribeInternal(subscriber, messageType, ct), cancellationToken: cancellationToken);
        }

        async Task SubscribeInternal(Subscriber subscriber, MessageType messageType, CancellationToken cancellationToken = default)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                await session.SaveOrUpdateAsync(new Subscription
                {
                    SubscriberEndpoint = subscriber.TransportAddress,
                    LogicalEndpoint = subscriber.Endpoint,
                    MessageType = messageType.TypeName + "," + messageType.Version,
                    Version = messageType.Version.ToString(),
                    TypeName = messageType.TypeName
                }, cancellationToken)
                    .ConfigureAwait(false);
                await tx.CommitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public virtual Task Unsubscribe(Subscriber address, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
        {
            return Retry(ct => UnsubscribeInternal(address, messageType, ct), cancellationToken: cancellationToken);
        }

        async Task UnsubscribeInternal(Subscriber address, MessageType messageType, CancellationToken cancellationToken = default)
        {
            var messageTypes = new List<MessageType>
            {
                messageType
            };
            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var transportAddress = address.TransportAddress;
                var subscriptions = await session.QueryOver<Subscription>()
                    .Where(
                        s => s.TypeName.IsIn(messageTypes.Select(mt => mt.TypeName).ToList()) &&
                             s.SubscriberEndpoint == transportAddress)
                             .ListAsync(cancellationToken).ConfigureAwait(false);

                foreach (var subscription in subscriptions.Where(s => messageTypes.Contains(new MessageType(s.TypeName, s.Version))))
                {
                    await session.DeleteAsync(subscription, cancellationToken).ConfigureAwait(false);
                }

                await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        static async Task Retry(Func<CancellationToken, Task> function, int maximumAttempts = 5, CancellationToken cancellationToken = default)
        {
            var attempt = 0;
            while (true)
            {
                try
                {
                    await function(cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Exception)
                {
                    attempt++;
                    if (attempt >= maximumAttempts)
                    {
                        throw;
                    }
                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public virtual async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context, CancellationToken cancellationToken = default)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            using (var session = sessionFactory.OpenStatelessSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var tmp = await session.QueryOver<Subscription>()
                    .Where(s => s.TypeName.IsIn(messageTypes.Select(mt => mt.TypeName).ToList()))
                    .ListAsync(cancellationToken)
                    .ConfigureAwait(false);

                await tx.CommitAsync(cancellationToken)
                    .ConfigureAwait(false);

                return tmp
                    .Where(s => messageTypes.Contains(new MessageType(s.TypeName, s.Version)))
                    .Distinct(SubscriptionComparer)
                    .Select(s => new Subscriber(s.SubscriberEndpoint, s.LogicalEndpoint));
            }
        }

        internal async Task Init(CancellationToken cancellationToken = default)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            using (var session = sessionFactory.OpenStatelessSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var v2XSubscriptions = await session.QueryOver<Subscription>()
                    .Where(s => s.TypeName == null)
                    .ListAsync(cancellationToken).ConfigureAwait(false);

                if (v2XSubscriptions.Count == 0)
                {
                    return;
                }

                Logger.DebugFormat("Found {0} v2X subscriptions going to upgrade", v2XSubscriptions.Count);

                foreach (var v2XSubscription in v2XSubscriptions)
                {
                    var mt = new MessageType(v2XSubscription.MessageType);
                    v2XSubscription.Version = mt.Version.ToString();
                    v2XSubscription.TypeName = mt.TypeName;

                    await session.UpdateAsync(v2XSubscription, cancellationToken)
                        .ConfigureAwait(false);
                }

                await tx.CommitAsync(cancellationToken)
                    .ConfigureAwait(false);
                Logger.InfoFormat("{0} v2X subscriptions upgraded", v2XSubscriptions.Count);
            }
        }

        class SubscriptionByTransportAddressComparer : IEqualityComparer<Subscription>
        {
            public bool Equals(Subscription x, Subscription y)
            {
                return x.SubscriberEndpoint.Equals(y.SubscriberEndpoint);
            }

            public int GetHashCode(Subscription obj)
            {
                return obj.SubscriberEndpoint.GetHashCode();
            }
        }
    }
}