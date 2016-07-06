namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using global::NHibernate;
    using global::NHibernate.Criterion;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using IsolationLevel = System.Data.IsolationLevel;

    class OutboxPersister : IOutboxStorage
    {
        public OutboxPersister(ISessionFactory sessionFactory, string endpointName)
        {
            this.sessionFactory = sessionFactory;
            this.endpointName = endpointName;
        }

        public Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            object[] possibleIds =
            {
                EndpointQualifiedMessageId(messageId),
                messageId
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
                            .Add(Restrictions.In(nameof(OutboxRecord.MessageId), possibleIds))
                            .UniqueResult<OutboxRecord>();

                        tx.Commit();
                    }
                }

                if (result == null)
                {
                    return Task.FromResult<OutboxMessage>(null);
                }
                if (result.Dispatched)
                {
                    return Task.FromResult(new OutboxMessage(result.MessageId, emptyTransportOperations));
                }
                var outboxOperations = ConvertStringToObject(result.TransportOperations);

                var transportOperations = new TransportOperation[outboxOperations.Length];
                var index = 0;
                foreach (var operation in outboxOperations)
                {
                    transportOperations[index] = new TransportOperation(operation.MessageId, operation.Options, operation.Message, operation.Headers);
                    index++;
                }

                var message = new OutboxMessage(result.MessageId, transportOperations);
                return Task.FromResult(message);
            }
        }

        public Task Store(OutboxMessage outboxMessage, OutboxTransaction transaction, ContextBag context)
        {
            var operations = new OutboxOperation[outboxMessage.TransportOperations.Length];
            var index = 0;
            foreach (var operation in outboxMessage.TransportOperations)
            {
                operations[index] = new OutboxOperation
                {
                    Message = operation.Body,
                    Headers = operation.Headers,
                    MessageId = operation.MessageId,
                    Options = operation.Options
                };
                index++;
            }

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
                        var queryString = $"update {typeof(OutboxRecord).Name} set Dispatched = true, DispatchedAt = :date, TransportOperations = NULL where MessageId IN ( :messageid, :qualifiedMessageId ) And Dispatched = false";
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
                        var queryString = $"delete from {typeof(OutboxRecord).Name} where Dispatched = true And DispatchedAt < :date";

                        session.CreateQuery(queryString)
                            .SetDateTime("date", dateTime)
                            .ExecuteUpdate();

                        tx.Commit();
                    }
                }
            }
        }

        static OutboxOperation[] ConvertStringToObject(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return emptyOutboxOperations;
            }

            return ObjectSerializer.DeSerialize<OutboxOperation[]>(data);
        }

        static string ConvertObjectToString(OutboxOperation[] operations)
        {
            if (operations == null || !operations.Any())
            {
                return null;
            }

            return ObjectSerializer.Serialize(operations);
        }

        string EndpointQualifiedMessageId(string messageId)
        {
            return $"{endpointName}/{messageId}";
        }

        string endpointName;
        ISessionFactory sessionFactory;

        static OutboxOperation[] emptyOutboxOperations = new OutboxOperation[0];
        static TransportOperation[] emptyTransportOperations = new TransportOperation[0];
    }
}