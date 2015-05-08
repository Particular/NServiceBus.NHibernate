namespace NServiceBus.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Transactions;
    using global::NHibernate.Criterion;
    using NHibernate;
    using Persistence.NHibernate;
    using Serializers.Json;
    using IsolationLevel = System.Data.IsolationLevel;

    class OutboxPersister : IOutboxStorage
    {
        public IStorageSessionProvider StorageSessionProvider { get; set; }
        public SessionFactoryProvider SessionFactoryProvider { get; set; }

        public bool TryGet(string messageId, out OutboxMessage message)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                OutboxRecord result;
                message = null;
                using (var session = SessionFactoryProvider.SessionFactory.OpenStatelessSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        //Explicitly using ICriteria instead of QueryOver for performance reasons.
                        //It seems QueryOver uses quite a bit reflection and that takes longer.
                        result = session.CreateCriteria<OutboxRecord>().Add(Expression.Eq("MessageId", messageId))
                            .UniqueResult<OutboxRecord>();

                        tx.Commit();
                    }
                }

                if (result == null)
                {
                    return false;
                }

                message = new OutboxMessage(result.MessageId);

                var operations = ConvertStringToObject(result.TransportOperations);
                message.TransportOperations.AddRange(operations.Select(t => new TransportOperation(t.MessageId,
                    t.Options, t.Message, t.Headers)));

                return true;
            }
        }

        public void Store(string messageId, IEnumerable<TransportOperation> transportOperations)
        {
            var operations = transportOperations.Select(t => new OutboxOperation
            {
                Message = t.Body,
                Headers = t.Headers,
                MessageId = t.MessageId,
                Options = t.Options,
            });

            StorageSessionProvider.ExecuteInTransaction(x => x.Save(new OutboxRecord
            {
                MessageId = messageId,
                Dispatched = false,
                TransportOperations = ConvertObjectToString(operations)
            }));
        }

        public void SetAsDispatched(string messageId)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                using (var session = SessionFactoryProvider.SessionFactory.OpenStatelessSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        var queryString = string.Format("update {0} set Dispatched = true, DispatchedAt = :date where MessageId = :messageid And Dispatched = false",
                            typeof(OutboxRecord));
                        session.CreateQuery(queryString)
                            .SetString("messageid", messageId)
                            .SetDateTime("date", DateTime.UtcNow)
                            .ExecuteUpdate();

                        tx.Commit();
                    }
                }
            }
        }

        public void RemoveEntriesOlderThan(DateTime dateTime)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                using (var session = SessionFactoryProvider.SessionFactory.OpenStatelessSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        var result = session.QueryOver<OutboxRecord>().Where(o => o.Dispatched && o.DispatchedAt < dateTime)
                            .List();

                        foreach (var record in result)
                        {
                            session.Delete(record);
                        }

                        tx.Commit();
                    }
                }
            }
        }

        static IEnumerable<OutboxOperation> ConvertStringToObject(string data)
        {
            if (String.IsNullOrEmpty(data))
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

        static readonly JsonMessageSerializer serializer = new JsonMessageSerializer(null);
    }
}
