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
    using NServiceBus.Logging;
    using NServiceBus.Outbox;
    using NServiceBus.Outbox.NHibernate;
    using IsolationLevel = System.Data.IsolationLevel;

    class OutboxPersister<TEntity> : INHibernateOutboxStorage
        where TEntity : class, IOutboxRecord, new()
    {
        static ILog Log = LogManager.GetLogger<OutboxPersister<TEntity>>();
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

            if (Transaction.Current != null)
            {
                Log.Warn("The endpoint is configured to use Outbox but a TransactionScope has been detected. Outbox mode is not compatible with "
                    + $"TransactionScope. Do not configure the transport to use '{nameof(TransportTransactionMode.TransactionScope)}' transaction mode with Outbox.");
            }

            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                TEntity result;
                using (var session = sessionFactory.OpenStatelessSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        //Explicitly using ICriteria instead of QueryOver for performance reasons.
                        //It seems QueryOver uses quite a bit reflection and that takes longer.
                        result = session.CreateCriteria<TEntity>()
                            .Add(Restrictions.In(nameof(IOutboxRecord.MessageId), possibleIds))
                            .UniqueResult<TEntity>();

                        tx.Commit();
                    }
                }

                if (result == null)
                {
                    return Task.FromResult<OutboxMessage>(null);
                }
                if (result.Dispatched)
                {
                    return Task.FromResult(new OutboxMessage(result.MessageId, new TransportOperation[0]));
                }
                var transportOperations = ConvertStringToObject(result.TransportOperations)
                    .Select(t => new TransportOperation(t.MessageId, t.Options, t.Message, t.Headers))
                    .ToArray();

                var message = new OutboxMessage(result.MessageId, transportOperations);
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
            var nhibernateTransaction = (NHibernateOutboxTransaction)transaction;
            var record = new TEntity
            {
                MessageId = EndpointQualifiedMessageId(outboxMessage.MessageId),
                Dispatched = false,
                TransportOperations = ConvertObjectToString(operations)
            };
            nhibernateTransaction.Session.Save(record);
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
                        var queryString = $"update {typeof(TEntity).Name} set Dispatched = true, DispatchedAt = :date, TransportOperations = NULL where MessageId IN ( :messageid, :qualifiedMessageId ) And Dispatched = false";
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
                        var queryString = $"delete from {typeof(TEntity).Name} where Dispatched = true And DispatchedAt < :date";

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

            return ObjectSerializer.DeSerialize<IEnumerable<OutboxOperation>>(data);
        }

        static string ConvertObjectToString(IEnumerable<OutboxOperation> operations)
        {
            if (operations == null || !operations.Any())
            {
                return null;
            }

            return ObjectSerializer.Serialize(operations);
        }

        string EndpointQualifiedMessageId(string messageId)
        {
            return endpointName + "/" + messageId;
        }
    }
}
