namespace NServiceBus.TimeoutPersisters.NHibernate
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using global::NHibernate;
    using IdGeneration;
    using Persistence.NHibernate;
    using Serializers.Json;
    using Timeout.Core;
    using IsolationLevel = System.Data.IsolationLevel;

    /// <summary>
    /// Timeout storage implementation for NHibernate.
    /// </summary>
    public class TimeoutStorage : IPersistTimeouts, IPersistTimeoutsV2
    {
        /// <summary>
        /// The current <see cref="ISessionFactory"/>.
        /// </summary>
        public ISessionFactory SessionFactory { get; set; }

        /// <summary>
        /// Retrieves the next range of timeouts that are due.
        /// </summary>
        /// <param name="startSlice">The time where to start retrieving the next slice, the slice should exclude this date.</param>
        /// <param name="nextTimeToRunQuery">Returns the next time we should query again.</param>
        /// <returns>Returns the next range of timeouts that are due.</returns>
        public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            var now = DateTime.UtcNow;
            
            using (var conn = SessionFactory.GetConnection())
            using (var session = SessionFactory.OpenStatelessSessionEx(conn))
            using (var tx = session.BeginAmbientTransactionAware(IsolationLevel.ReadCommitted))
            {
                var results = session.QueryOver<TimeoutEntity>()
                    .Where(x => x.Endpoint == Configure.EndpointName)
                    .And(x => x.Time >= startSlice && x.Time <= now)
                    .OrderBy(x => x.Time).Asc
                    .Select(x => x.Id, x => x.Time)
                    .List<object[]>()
                    .Select(properties => new Tuple<string, DateTime>(((Guid) properties[0]).ToString(), (DateTime) properties[1]))
                    .ToList();

                //Retrieve next time we need to run query
                var startOfNextChunk = session.QueryOver<TimeoutEntity>()
                    .Where(x => x.Endpoint == Configure.EndpointName)
                    .Where(x => x.Time > now)
                    .OrderBy(x => x.Time).Asc
                    .Take(1)
                    .SingleOrDefault();

                if (startOfNextChunk != null)
                {
                    nextTimeToRunQuery = startOfNextChunk.Time;
                }
                else
                {
                    nextTimeToRunQuery = DateTime.UtcNow.AddMinutes(10);
                }

                tx.Commit();

                return results;
            }
        }

        /// <summary>
        /// Adds a new timeout.
        /// </summary>
        /// <param name="timeout">Timeout data.</param>
        public void Add(TimeoutData timeout)
        {
            var timeoutId = CombGuid.Generate();

            using (var conn = SessionFactory.GetConnection())
            using (var session = SessionFactory.OpenSessionEx(conn))
            using (var tx = session.BeginAmbientTransactionAware(IsolationLevel.ReadCommitted))
            {
                session.Save(new TimeoutEntity
                {
                    Id = timeoutId,
                    CorrelationId = timeout.CorrelationId,
                    Destination = timeout.Destination,
                    SagaId = timeout.SagaId,
                    State = timeout.State,
                    Time = timeout.Time,
                    Headers = ConvertDictionaryToString(timeout.Headers),
                    Endpoint = timeout.OwningTimeoutManager,
                });

                tx.Commit();
            }

            timeout.Id = timeoutId.ToString();
        }

        /// <summary>
        /// Removes the timeout if it hasn't been previously removed.
        /// </summary>
        /// <param name="timeoutId">The timeout id to remove.</param>
        /// <param name="timeoutData">The timeout data of the removed timeout.</param>
        /// <returns><c>true</c> it the timeout was successfully removed.</returns>
        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            int result;

            using (var conn = SessionFactory.GetConnection())
            using (var session = SessionFactory.OpenStatelessSessionEx(conn))
            using (var tx = session.BeginAmbientTransactionAware(IsolationLevel.ReadCommitted))
            {
                var te = session.Get<TimeoutEntity>(new Guid(timeoutId));

                if (te == null)
                {
                    tx.Commit();
                    timeoutData = null;
                    return false;
                }

                timeoutData = MapToTimeoutData(te);

                var queryString = string.Format("delete {0} where Id = :id",
                                        typeof(TimeoutEntity));
                result = session.CreateQuery(queryString)
                       .SetParameter("id", new Guid(timeoutId))
                       .ExecuteUpdate();

                tx.Commit();
            }

            if (result == 0)
            {
                timeoutData = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes the time by saga id.
        /// </summary>
        /// <param name="sagaId">The saga id of the timeouts to remove.</param>
        public void RemoveTimeoutBy(Guid sagaId)
        {
            using (var conn = SessionFactory.GetConnection())
            using (var session = SessionFactory.OpenStatelessSessionEx(conn))
            using (var tx = session.BeginAmbientTransactionAware(IsolationLevel.ReadCommitted))
            {
                var queryString = string.Format("delete {0} where SagaId = :sagaid",
                                        typeof(TimeoutEntity));
                session.CreateQuery(queryString)
                       .SetParameter("sagaid", sagaId)
                       .ExecuteUpdate();

                tx.Commit();
            }
        }

        static Dictionary<string,string> ConvertStringToDictionary(string data)
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

        public TimeoutData Peek(string timeoutId)
        {
            TimeoutData timeoutData = null;

            using (var conn = SessionFactory.GetConnection())
            using (var session = SessionFactory.OpenStatelessSessionEx(conn))
            using (var tx = session.BeginAmbientTransactionAware(IsolationLevel.ReadCommitted))
            {
                var te = session.Get<TimeoutEntity>(new Guid(timeoutId), LockMode.Upgrade);

                if (te != null)
                {
                    timeoutData = MapToTimeoutData(te);
                }

                tx.Commit();
            }

            return timeoutData;
        }

        static TimeoutData MapToTimeoutData(TimeoutEntity te)
        {
            return new TimeoutData
            {
                CorrelationId = te.CorrelationId,
                Destination = te.Destination,
                Id = te.Id.ToString(),
                SagaId = te.SagaId,
                State = te.State,
                Time = te.Time,
                Headers = ConvertStringToDictionary(te.Headers),
            };
        }

        public bool TryRemove(string timeoutId)
        {
            TimeoutData timeoutData;
            return this.TryRemove(timeoutId, out timeoutData);
        }

        static readonly JsonMessageSerializer serializer = new JsonMessageSerializer(null);
    }
}
