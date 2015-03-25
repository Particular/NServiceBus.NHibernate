namespace NServiceBus.Unicast.Subscriptions.NHibernate
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using global::NHibernate;
    using global::NHibernate.Criterion;
    using Logging;
    using MessageDrivenSubscriptions;
    using NServiceBus.Extensibility;
    using NServiceBus.Routing;
    using IsolationLevel = System.Data.IsolationLevel;

    /// <summary>
    ///     Subscription storage using NHibernate for persistence
    /// </summary>
    class SubscriptionPersister : ISubscriptionStorage
    {

        static readonly ILog Logger = LogManager.GetLogger(typeof(ISubscriptionStorage));
        static readonly IEqualityComparer<Subscription> SubscriptionComparer = new SubscriptionByTransportAddressComparer();

        ISessionFactory sessionFactory;

        public SubscriptionPersister(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public virtual Task Subscribe(Subscriber subscriber, IReadOnlyCollection<MessageType> messageTypes, ContextBag context)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                using (var session = sessionFactory.OpenSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        foreach (var messageType in messageTypes)
                        {
                            session.SaveOrUpdate(new Subscription
                            {
                                SubscriberEndpoint = subscriber.TransportAddress,
                                LogicalEndpoint = subscriber.Endpoint != null ? subscriber.Endpoint.ToString() : null,
                                MessageType = messageType.TypeName + "," + messageType.Version,
                                Version = messageType.Version.ToString(),
                                TypeName = messageType.TypeName
                            });
                        }

                        tx.Commit();
                    }
                }
            }
            return Task.FromResult(0);
        }

        public virtual Task Unsubscribe(Subscriber address, IReadOnlyCollection<MessageType> messageTypes, ContextBag context)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                using (var session = sessionFactory.OpenSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        var transportAddress = address.TransportAddress;
                        var subscriptions = session.QueryOver<Subscription>()
                            .Where(
                                s => s.TypeName.IsIn(messageTypes.Select(mt => mt.TypeName).ToList()) &&
                                     s.SubscriberEndpoint == transportAddress)
                            .List();

                        foreach (var subscription in subscriptions.Where(s => messageTypes.Contains(new MessageType(s.TypeName, s.Version))))
                        {
                            session.Delete(subscription);
                        }

                        tx.Commit();
                    }
                }
            }
            return Task.FromResult(0);
        }

        public virtual Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IReadOnlyCollection<MessageType> messageTypes, ContextBag context)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                using (var session = sessionFactory.OpenStatelessSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        var tmp = session.QueryOver<Subscription>()
                            .Where(s => s.TypeName.IsIn(messageTypes.Select(mt => mt.TypeName).ToList()))
                            .List();

                        var results = tmp
                            .Where(s => messageTypes.Contains(new MessageType(s.TypeName, s.Version)))
                            .Distinct(SubscriptionComparer)
                            .Select(s => new Subscriber(s.SubscriberEndpoint, s.LogicalEndpoint != null ? new EndpointName(s.LogicalEndpoint) : null))
                            .ToList();

                        tx.Commit();

                        return Task.FromResult(results.AsEnumerable());
                    }
                }
            }
        }

        public void Init()
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                using (var session = sessionFactory.OpenStatelessSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        var v2XSubscriptions = session.QueryOver<Subscription>()
                            .Where(s => s.TypeName == null)
                            .List();
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

                            session.Update(v2XSubscription);
                        }

                        tx.Commit();
                        Logger.InfoFormat("{0} v2X subscriptions upgraded", v2XSubscriptions.Count);
                    }
                }
            }
        }

        private class SubscriptionByTransportAddressComparer : IEqualityComparer<Subscription>
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