namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using global::NHibernate;
    using global::NHibernate.Criterion;
    using Extensibility;
    using Outbox;
    using Outbox.NHibernate;
    using IsolationLevel = System.Data.IsolationLevel;

    class OutboxPersister<TEntity> : INHibernateOutboxStorage
        where TEntity : class, IOutboxRecord, new()
    {
        const string EndpointQualifiedMessageIdContextKey = "NServiceBus.Persistence.NHibernate.EndpointQualifiedMessageId";
        ISessionFactory sessionFactory;
        Func<INHibernateOutboxTransaction> outboxTransactionFactory;
        string endpointName;

        public OutboxPersister(ISessionFactory sessionFactory, Func<INHibernateOutboxTransaction> outboxTransactionFactory, string endpointName)
        {
            this.sessionFactory = sessionFactory;
            this.outboxTransactionFactory = outboxTransactionFactory;
            this.endpointName = endpointName;
        }

        public async Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            object[] possibleIds = {
                EndpointQualifiedMessageId(messageId),
                messageId,
            };

            if (Transaction.Current != null)
            {
                throw new Exception("The endpoint is configured to use Outbox but a TransactionScope has been detected. Outbox mode is not compatible with "
                    + $"TransactionScope. Do not configure the transport to use '{nameof(TransportTransactionMode.TransactionScope)}' transaction mode with Outbox.");
            }

            //Required by BeginTransaction
            context.Set(EndpointQualifiedMessageIdContextKey, EndpointQualifiedMessageId(messageId));

            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            {
                TEntity result;
                using (var session = sessionFactory.OpenStatelessSession())
                using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    //Explicitly using ICriteria instead of QueryOver for performance reasons.
                    //It seems QueryOver uses quite a bit reflection and that takes longer.
                    result = await session.CreateCriteria<TEntity>()
                        .Add(Restrictions.In(nameof(IOutboxRecord.MessageId), possibleIds))
                        .UniqueResultAsync<TEntity>()
                        .ConfigureAwait(false);

                    await tx.CommitAsync()
                        .ConfigureAwait(false);
                }

                if (result == null)
                {
                    return null;
                }
                if (result.Dispatched)
                {
                    return new OutboxMessage(result.MessageId, new TransportOperation[0]);
                }
                var transportOperations = ConvertStringToObject(result.TransportOperations)
                    .Select(t => new TransportOperation(t.MessageId, t.Options, t.Message, t.Headers))
                    .ToArray();

                return new OutboxMessage(result.MessageId, transportOperations);
            }
        }

        public Task Store(OutboxMessage outboxMessage, OutboxTransaction transaction, ContextBag context)
        {
            var nhibernateTransaction = (INHibernateOutboxTransaction)transaction;
            return nhibernateTransaction.Complete(EndpointQualifiedMessageId(outboxMessage.MessageId), outboxMessage, context);
        }

        public async Task SetAsDispatched(string messageId, ContextBag context)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            using (var session = sessionFactory.OpenStatelessSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var queryString = $"update {typeof(TEntity).Name} set Dispatched = true, DispatchedAt = :date, TransportOperations = NULL where MessageId IN ( :messageid, :qualifiedMessageId ) And Dispatched = false";
                await session.CreateQuery(queryString)
                    .SetString("messageid", messageId)
                    .SetString("qualifiedMessageId", EndpointQualifiedMessageId(messageId))
                    .SetDateTime("date", DateTime.UtcNow)
                    .ExecuteUpdateAsync()
                    .ConfigureAwait(false);

                await tx.CommitAsync()
                    .ConfigureAwait(false);
            }
        }

        public Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            //Provided by Get
            var endpointQualifiedMessageId = context.Get<string>(EndpointQualifiedMessageIdContextKey);
            var result = outboxTransactionFactory();
            result.Prepare();
            return result.Begin(endpointQualifiedMessageId);
        }

        public async Task RemoveEntriesOlderThan(DateTime dateTime)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            using (var session = sessionFactory.OpenStatelessSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var queryString = $"delete from {typeof(TEntity).Name} where Dispatched = true And DispatchedAt < :date";

                await session.CreateQuery(queryString)
                    .SetDateTime("date", dateTime)
                    .ExecuteUpdateAsync().ConfigureAwait(false);

                await tx.CommitAsync().ConfigureAwait(false);
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

        string EndpointQualifiedMessageId(string messageId)
        {
            return endpointName + "/" + messageId;
        }
    }
}