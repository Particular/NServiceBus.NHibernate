namespace NServiceBus.Unicast.Subscriptions.NHibernate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using global::NHibernate;
    using global::NHibernate.Criterion;
    using global::NHibernate.Exceptions;
    using Logging;
    using MessageDrivenSubscriptions;
    using NServiceBus.Extensibility;
    using IsolationLevel = System.Data.IsolationLevel;
    using TransactionException = global::NHibernate.TransactionException;

    class SubscriptionPersister : ISubscriptionStorage
    {
        const int MaxRetries = 5;

        static ILog Logger = LogManager.GetLogger(typeof(ISubscriptionStorage));
        static IEqualityComparer<Subscription> SubscriptionComparer = new SubscriptionByTransportAddressComparer();
        ISessionFactory sessionFactory;

        public SubscriptionPersister(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public virtual Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            for (var attempt = 1; attempt <= MaxRetries; ++attempt)
            {
                try
                {
                    StoreSubscription(subscriber, messageType);
                    return Task.FromResult(0);
                }
                catch (Exception e) when(e is TransactionException || e is GenericADOException)
                {
                    // A unique constraint violation exception is possible at this point in a scale-out scenario.

                    if (attempt < MaxRetries)
                    {
                        // An exception will be swallowed here to allow for a retry.
                        Logger.DebugFormat("Error occured when storing subscription of endpoint '{0}' to message '{1}'. The operation will be retried.", subscriber.Endpoint, messageType);
                    }
                    else
                    {
                        // This was the last attempt, give up.
                        throw;
                    }
                }
            }

            throw new Exception($"Internal error occured when storing subscription of endpoint '{subscriber.Endpoint}' to message '{messageType}'.");
        }

        void StoreSubscription(Subscriber subscriber, MessageType messageType)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                session.SaveOrUpdate(new Subscription
                {
                    SubscriberEndpoint = subscriber.TransportAddress,
                    LogicalEndpoint = subscriber.Endpoint,
                    MessageType = messageType.TypeName + "," + messageType.Version,
                    Version = messageType.Version.ToString(),
                    TypeName = messageType.TypeName
                });
                tx.Commit();
            }
        }

        public virtual Task Unsubscribe(Subscriber address, MessageType messageType, ContextBag context)
        {
            for (var attempt = 1; attempt <= MaxRetries; ++attempt)
            {
                try
                {
                    DeleteSubscription(address, messageType);
                    return Task.FromResult(0);
                }
                catch (Exception e) when (e is TransactionException || e is GenericADOException)
                {
                    // An aborted transaction is possible at this point in a scale-out scenario.

                    if (attempt < MaxRetries)
                    {
                        // An exception will be swallowed here to allow for a retry.
                        Logger.DebugFormat("Error occured when deleting subscription of endpoint '{0}' to message '{1}'. The operation will be retried.", address.Endpoint, messageType);
                    }
                    else
                    {
                        // This was the last attempt, give up.
                        throw;
                    }
                }
            }

            throw new Exception($"Internal error occured when deleting subscription of endpoint '{address.Endpoint}' to message '{messageType}'.");
        }

        void DeleteSubscription(Subscriber address, MessageType messageType)
        {
            var messageTypes = new List<MessageType> { messageType };
            using (new TransactionScope(TransactionScopeOption.Suppress))
            using (var session = sessionFactory.OpenSession())
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

        public virtual Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            using (var session = sessionFactory.OpenStatelessSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var tmp = session.QueryOver<Subscription>()
                    .Where(s => s.TypeName.IsIn(messageTypes.Select(mt => mt.TypeName).ToList()))
                    .List();

                var results = tmp
                    .Where(s => messageTypes.Contains(new MessageType(s.TypeName, s.Version)))
                    .Distinct(SubscriptionComparer)
                    .Select(s => new Subscriber(s.SubscriberEndpoint, s.LogicalEndpoint))
                    .ToList();

                tx.Commit();

                return Task.FromResult(results.AsEnumerable());
            }
        }

        internal void Init()
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            using (var session = sessionFactory.OpenStatelessSession())
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