namespace NServiceBus.Outbox
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using global::NHibernate;
    using NHibernate;
    using Persistence;
    using Persistence.NHibernate;
    using Serializers.Json;
    using Unicast;
    using UnitOfWork.NHibernate;

    class OutboxPersister : IOutboxStorage
    {
        public ISessionFactory SessionFactory { get; set; }

        public IStorageSessionProvider StorageSessionProvider { get; set; }

        public IDbConnectionProvider DbConnectionProvider { get; set; }

        public bool TryGet(string messageId, out OutboxMessage message)
        {
            OutboxRecord result;

            message = null;

            using (var session = SessionFactory.OpenStatelessSessionEx(DbConnectionProvider.Connection))
            {
                using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
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

            message = new OutboxMessage(result.MessageId, result.Dispatched);
            message.TransportOperations.AddRange(result.TransportOperations.Select(t => new TransportOperation(
                new SendOptions(t.Destination)
                {
                    CorrelationId = t.CorrelationId,
                    DelayDeliveryWith = t.DelayDeliveryWith,
                    DeliverAt = t.DeliverAt,
                    EnforceMessagingBestPractices = t.EnforceMessagingBestPractices,
                    Intent = t.Intent,
                    ReplyToAddress = t.ReplyToAddress,
                },
                new TransportMessage(t.MessageId, ConvertStringToDictionary(t.Headers)), t.MessageType)));

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
                    Intent = t.SendOptions.Intent,
                    Message = t.Message.Body,
                    MessageType = t.MessageType,
                    CorrelationId = t.SendOptions.CorrelationId,
                    DelayDeliveryWith = t.SendOptions.DelayDeliveryWith,
                    DeliverAt = t.SendOptions.DeliverAt,
                    Destination = t.SendOptions.Destination,
                    EnforceMessagingBestPractices = t.SendOptions.EnforceMessagingBestPractices,
                    ReplyToAddress = t.SendOptions.ReplyToAddress,
                    Headers = ConvertDictionaryToString(t.Message.Headers),
                    MessageId = t.Message.Id,
                }).ToList()
            });
        }

        public void SetAsDispatched(string messageId)
        {
            int result;
         
            using (var session = SessionFactory.OpenStatelessSessionEx(DbConnectionProvider.Connection))
            {
                using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    var queryString = string.Format("update {0} set Dispatched = true, DispatchedAt = :date where MessageId = :messageid And Dispatched = false",
                        typeof(OutboxRecord));
                    result = session.CreateQuery(queryString)
                        .SetParameter("messageid", messageId)
                        .SetParameter("date", DateTime.UtcNow)
                        .ExecuteUpdate();

                    tx.Commit();
                }
            }


            if (result == 0)
            {
                throw new ConcurrencyException(string.Format("Outbox message with id '{0}' is has already been updated by another thread.", messageId));
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