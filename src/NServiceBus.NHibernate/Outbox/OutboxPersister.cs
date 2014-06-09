namespace NServiceBus.Outbox
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using NHibernate;
    using NServiceBus.NHibernate.Internal;
    using NServiceBus.NHibernate.SharedSession;
    using Serializers.Json;

    class OutboxPersister : IOutboxStorage
    {
        public IStorageSessionProvider StorageSessionProvider { get; set; }

        public bool TryGet(string messageId, out OutboxMessage message)
        {
            OutboxRecord result;

            message = null;

            using (var session = StorageSessionProvider.OpenStatelessSession())
            {
                using (var tx = session.BeginAmbientTransactionAware(IsolationLevel.ReadCommitted))
                {
                    result = session.QueryOver<OutboxRecord>().Where(o => o.MessageId == messageId)
                        .Fetch(entity => entity.TransportOperations).Eager
                        .SingleOrDefault();

                    tx.Commit();
                }
            }

            if (result == null)
            {
                return false;
            }

            message = new OutboxMessage(result.MessageId);
            message.TransportOperations.AddRange(result.TransportOperations.Select(t => new TransportOperation(t.MessageId, 
                ConvertStringToDictionary(t.Options), t.Message, ConvertStringToDictionary(t.Headers))));

            return true;
        }

        public void Store(string messageId, IEnumerable<TransportOperation> transportOperations)
        {
            StorageSessionProvider.Session.Save(new OutboxRecord
            {
                MessageId = messageId,
                Dispatched = false,
                TransportOperations = transportOperations.Select(t => new OutboxOperation
                {
                    Message = t.Body,
                    Headers = ConvertDictionaryToString(t.Headers),
                    MessageId = t.MessageId,
                    Options = ConvertDictionaryToString(t.Options),
                }).ToList()
            });
        }

        public void SetAsDispatched(string messageId)
        {
            using (var session = StorageSessionProvider.OpenStatelessSession())
            {
                using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    var queryString = string.Format("update {0} set Dispatched = true, DispatchedAt = :date where MessageId = :messageid And Dispatched = false",
                        typeof(OutboxRecord));
                    session.CreateQuery(queryString)
                        .SetParameter("messageid", messageId)
                        .SetParameter("date", DateTime.UtcNow)
                        .ExecuteUpdate();
                    
                    queryString = string.Format("delete from {0} where MessageId = :messageid",
                        typeof(OutboxOperation));
                    session.CreateQuery(queryString)
                        .SetParameter("messageid", messageId)
                        .ExecuteUpdate();
                    
                    tx.Commit();
                }
            }
        }

        public void RemoveEntriesOlderThan(DateTime dateTime)
        {
            using (var session = StorageSessionProvider.OpenSession())
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

        static Dictionary<string, string> ConvertStringToDictionary(string data)
        {
            if (String.IsNullOrEmpty(data))
            {
                return new Dictionary<string, string>();
            }

            return serializer.DeserializeObject<Dictionary<string, string>>(data);
        }

        static string ConvertDictionaryToString(ICollection data)
        {
            if (data == null || data.Count == 0)
            {
                return null;
            }

            return serializer.SerializeObject(data);
        }

        static readonly JsonMessageSerializer serializer = new JsonMessageSerializer(null);
    }
}
