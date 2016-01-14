namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using global::NHibernate;
    using global::NHibernate.Criterion;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using Serializers.Json;
    using IsolationLevel = System.Data.IsolationLevel;

    class OutboxPersister : IOutboxStorage
    {
        static readonly JsonMessageSerializer serializer = new JsonMessageSerializer(null);
        ISessionFactory sessionFactory;
        string endpointName;

        public OutboxPersister(ISessionFactory sessionFactory, string endpointName)
        {
            this.sessionFactory = sessionFactory;
            this.endpointName = endpointName;
        }

        public Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            object[] possibleIds = {
                EndpointQualifiedMessageId(messageId),
                messageId,
            };

            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                OutboxRecord result;
                using (var session = sessionFactory.OpenStatelessSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        //Explicitly using ICriteria instead of QueryOver for performance reasons.
                        //It seems QueryOver uses quite a bit reflection and that takes longer.
                        result = session.CreateCriteria<OutboxRecord>()
                            .Add(Restrictions.In("MessageId", possibleIds))
                            .UniqueResult<OutboxRecord>();

                        tx.Commit();
                    }
                }

                if (result == null)
                {
                    return Task.FromResult<OutboxMessage>(null);
                }
                var transportOperations = ConvertStringToObject(result.TransportOperations)
                    .Select(t => new TransportOperation(t.MessageId, t.Options, t.Message, t.Headers))
                    .ToList();

                var message = new OutboxMessage(result.MessageId,transportOperations);
                return Task.FromResult(message);
            }
        }

        public Task Store(OutboxMessage outboxMessage, OutboxTransaction transaction, ContextBag context)
        {
            var operations = outboxMessage.TransportOperations.Select(t => new OutboxOperation
            {
                Message = t.Body,
                Headers = t.Headers,
                MessageId = t.MessageId,
                Options = t.Options,
            });
            var nhibernateTransaction = (NHibernateOutboxTransaction) transaction;
            nhibernateTransaction.Session.Save(new OutboxRecord
            {
                MessageId = EndpointQualifiedMessageId(outboxMessage.MessageId),
                Dispatched = false,
                TransportOperations = ConvertObjectToString(operations)
            });
            return Task.FromResult(0);
        }

        public Task SetAsDispatched(string messageId, ContextBag context)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                using (var session = sessionFactory.OpenStatelessSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        var queryString = $"update {typeof(OutboxRecord)} set Dispatched = true, DispatchedAt = :date where MessageId IN ( :messageid, :qualifiedMessageId ) And Dispatched = false";
                        session.CreateQuery(queryString)
                            .SetString("messageid", messageId)
                            .SetString("qualifiedMessageId", EndpointQualifiedMessageId(messageId))
                            .SetDateTime("date", DateTime.UtcNow)
                            .ExecuteUpdate();

                        tx.Commit();
                    }
                }
            }

            return Task.FromResult(0);
        }

        public Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            var session = sessionFactory.OpenSession();
            var transaction = session.BeginTransaction();

            OutboxTransaction result = new NHibernateOutboxTransaction(session, transaction);
            return Task.FromResult(result);
        }

        public void RemoveEntriesOlderThan(DateTime dateTime)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                using (var session = sessionFactory.OpenStatelessSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        var queryString = string.Format("delete from {0} where Dispatched = true And DispatchedAt < :date", typeof(OutboxRecord));

                        session.CreateQuery(queryString)
                            .SetDateTime("date", dateTime)
                            .ExecuteUpdate();

                        tx.Commit();
                    }
                }
            }
        }

        static IEnumerable<OutboxOperation> ConvertStringToObject(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return Enumerable.Empty<OutboxOperation>();
            }

            return (IEnumerable<OutboxOperation>)serializer.DeserializeObject(data, typeof(IEnumerable<OutboxOperation>));
        }

        static string ConvertObjectToString(IEnumerable<OutboxOperation> operations)
        {
            if (operations == null || !operations.Any())
            {
                return null;
            }

            return serializer.SerializeObject(operations);
        }

        string EndpointQualifiedMessageId(string messageId)
        {
            return endpointName + "/" + messageId;
        }
    }
}
