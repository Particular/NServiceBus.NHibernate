namespace NServiceBus.Persistence.NHibernate
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
    using Outbox;
    using Outbox.NHibernate;
    using DispatchProperties = NServiceBus.Transport.DispatchProperties;
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

        public async Task<OutboxMessage> Get(string messageId, ContextBag context, CancellationToken cancellationToken = default)
        {
            object[] possibleIds =
            {
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
                        .UniqueResultAsync<TEntity>(cancellationToken)
                        .ConfigureAwait(false);

                    await tx.CommitAsync(cancellationToken)
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
                    .Select(t => new TransportOperation(t.MessageId, new DispatchProperties(t.Options), t.Message, t.Headers))
                    .ToArray();

                return new OutboxMessage(result.MessageId, transportOperations);
            }
        }

        public Task Store(OutboxMessage outboxMessage, OutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            var nhibernateTransaction = (INHibernateOutboxTransaction)transaction;
            return nhibernateTransaction.Complete(EndpointQualifiedMessageId(outboxMessage.MessageId), outboxMessage, context, cancellationToken);
        }

        public async Task SetAsDispatched(string messageId, ContextBag context, CancellationToken cancellationToken = default)
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
                    .ExecuteUpdateAsync(cancellationToken)
                    .ConfigureAwait(false);

                await tx.CommitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public Task<OutboxTransaction> BeginTransaction(ContextBag context, CancellationToken cancellationToken = default)
        {
            //Provided by Get
            var endpointQualifiedMessageId = context.Get<string>(EndpointQualifiedMessageIdContextKey);
            var result = outboxTransactionFactory();
            result.Prepare();
            // we always need to avoid using async/await in here so that the transaction scope can float!
            return BeginTransactionInternal(result, endpointQualifiedMessageId, cancellationToken);
        }

        static async Task<OutboxTransaction> BeginTransactionInternal(INHibernateOutboxTransaction transaction, string endpointQualifiedMessageId, CancellationToken cancellationToken)
        {
            try
            {
                await transaction.Begin(endpointQualifiedMessageId, cancellationToken).ConfigureAwait(false);
                return transaction;
            }
            catch (Exception e)
            {
                // A method that returns something that is disposable should not throw during the creation
                // of the disposable resource. If it does the compiler generated code will not dispose anything
                // therefore we need to dispose here to prevent the connection being returned to the pool being
                // in a zombie state.
                transaction.Dispose();
                throw new Exception("Error while opening outbox transaction", e);
            }
        }

        public async Task RemoveEntriesOlderThan(DateTime dateTime, CancellationToken cancellationToken = default)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            using (var session = sessionFactory.OpenStatelessSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var queryString = $"delete from {typeof(TEntity).Name} where Dispatched = true And DispatchedAt < :date";

                await session.CreateQuery(queryString)
                    .SetDateTime("date", dateTime)
                    .ExecuteUpdateAsync(cancellationToken).ConfigureAwait(false);

                await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
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