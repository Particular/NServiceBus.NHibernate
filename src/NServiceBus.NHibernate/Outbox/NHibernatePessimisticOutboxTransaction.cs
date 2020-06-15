namespace NServiceBus.Outbox.NHibernate
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using global::NHibernate;
    using Janitor;
    using Outbox;
    using Persistence.NHibernate;

    [SkipWeaving]
    class NHibernatePessimisticOutboxTransaction : INHibernateOutboxTransaction
    {
        public NHibernatePessimisticOutboxTransaction(ISession session, ITransaction transaction)
        {
            Session = session;
            this.transaction = transaction;
        }

        public ISession Session { get; }

        public ITransaction Transaction => transaction;

        public void OnSaveChanges(Func<Task> callback)
        {
            if (onSaveChangesCallback != null)
            {
                throw new Exception("Save changes callback for this session has already been registered.");
            }
            onSaveChangesCallback = callback;
        }

        public async Task Commit()
        {
            if (onSaveChangesCallback != null)
            {
                await onSaveChangesCallback().ConfigureAwait(false);
            }
            await transaction.CommitAsync().ConfigureAwait(false);
            transaction.Dispose();
            transaction = null;
        }

        public void Dispose()
        {
            //If save changes callback failed, we need to dispose the transaction here.
            if (transaction != null)
            {
                transaction.Dispose();
                transaction = null;
            }
            Session.Dispose();
        }

        Func<Task> onSaveChangesCallback;
        ITransaction transaction;

        public Task Open<TEntity>(string endpointQualifiedMessageId) where TEntity : class, IOutboxRecord, new()
        {
            var outboxRecord = new TEntity
            {
                MessageId = endpointQualifiedMessageId,
                Dispatched = false,
                TransportOperations = null
            };
            return Session.SaveAsync(outboxRecord);
        }

        public Task Complete<TEntity>(string endpointQualifiedMessageId, OutboxMessage outboxMessage, ContextBag context)
            where TEntity : class, IOutboxRecord, new()
        {
            var queryString = $"update {typeof(TEntity).Name} set TransportOperations = :ops where MessageId = :messageid";
            return Session.CreateQuery(queryString)
                .SetString("messageid", endpointQualifiedMessageId)
                .SetString("ops", ConvertOperations(outboxMessage))
                .ExecuteUpdateAsync();
        }

        static string ConvertOperations(OutboxMessage outboxMessage)
        {
            if (outboxMessage.TransportOperations.Length == 0)
            {
                return null;
            }
            var operations = outboxMessage.TransportOperations.Select(t => new OutboxOperation
            {
                Message = t.Body,
                Headers = t.Headers,
                MessageId = t.MessageId,
                Options = t.Options,
            });
            return ObjectSerializer.Serialize(operations);
        }
    }
}