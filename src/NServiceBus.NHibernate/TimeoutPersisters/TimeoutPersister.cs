namespace NServiceBus.TimeoutPersisters.NHibernate
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using global::NHibernate;
    using Outbox;
    using Persistence.NHibernate;
    using Serializers.Json;
    using Timeout.Core;

    class TimeoutPersister : IPersistTimeouts
    {
        public ISessionFactory SessionFactory { get; set; }

        public IDbConnectionProvider DbConnectionProvider { get; set; }
        public string EndpointName { get; set; }
        public string ConnectionString { get; set; }

        /// <summary>
        ///     Retrieves the next range of timeouts that are due.
        /// </summary>
        /// <param name="startSlice">The time where to start retrieving the next slice, the slice should exclude this date.</param>
        /// <param name="nextTimeToRunQuery">Returns the next time we should query again.</param>
        /// <returns>Returns the next range of timeouts that are due.</returns>
        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            var now = DateTime.UtcNow;

            using (var conn = SessionFactory.GetConnection())
            {
                using (var session = SessionFactory.OpenStatelessSessionEx(conn))
                {
                    using (var tx = session.BeginAmbientTransactionAware(IsolationLevel.ReadCommitted))
                    {
                        var results = session.QueryOver<TimeoutEntity>()
                            .Where(x => x.Endpoint == EndpointName)
                            .And(x => x.Time >= startSlice && x.Time <= now)
                            .OrderBy(x => x.Time).Asc
                            .Select(x => x.Id, x => x.Time)
                            .List<object[]>()
                            .Select(properties => new Tuple<string, DateTime>(((Guid)properties[0]).ToString(), (DateTime)properties[1]))
                            .ToList();

                        //Retrieve next time we need to run query
                        var startOfNextChunk = session.QueryOver<TimeoutEntity>()
                            .Where(x => x.Endpoint == EndpointName)
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
            }
        }


        /// <summary>
        ///     Adds a new timeout.
        /// </summary>
        /// <param name="timeout">Timeout data.</param>
        public void Add(TimeoutData timeout)
        {
            var timeoutId = GenerateCombGuid();
            IDbConnection connection;

            if (TryGetConnection(out connection))
            {
                StoreTimeoutEntity(timeout, connection, timeoutId);
            }
            else
            {
                using (connection = SessionFactory.GetConnection())
                {
                    StoreTimeoutEntity(timeout, connection, timeoutId);
                }
            }

            timeout.Id = timeoutId.ToString();
        }

        void StoreTimeoutEntity(TimeoutData timeout, IDbConnection connection, Guid timeoutId)
        {
            using (var session = SessionFactory.OpenSessionEx(connection))
            {
                using (var tx = session.BeginAmbientTransactionAware(IsolationLevel.ReadCommitted))
                {
                    session.Save(new TimeoutEntity
                    {
                        Id = timeoutId,
                        Destination = timeout.Destination,
                        SagaId = timeout.SagaId,
                        State = timeout.State,
                        Time = timeout.Time,
                        Headers = ConvertDictionaryToString(timeout.Headers),
                        Endpoint = timeout.OwningTimeoutManager,
                    });

                    tx.Commit();
                }
            }
        }

        /// <summary>
        ///     Removes the timeout if it hasn't been previously removed.
        /// </summary>
        /// <param name="timeoutId">The timeout id to remove.</param>
        /// <param name="timeoutData">The timeout data of the removed timeout.</param>
        /// <returns><c>true</c> it the timeout was successfully removed.</returns>
        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            IDbConnection connection;

            if (TryGetConnection(out connection))
            {
                return TryRemoveTimeoutEntity(Guid.Parse(timeoutId), connection, out timeoutData);
            }

            using (connection = SessionFactory.GetConnection())
            {
                return TryRemoveTimeoutEntity(Guid.Parse(timeoutId), connection, out timeoutData);
            }
        }

        bool TryRemoveTimeoutEntity(Guid timeoutId, IDbConnection connection, out TimeoutData timeoutData)
        {
            bool found;

            using (var session = SessionFactory.OpenStatelessSessionEx(connection))
            {
                using (var tx = session.BeginAmbientTransactionAware(IsolationLevel.ReadCommitted))
                {
                    var te = session.Get<TimeoutEntity>(timeoutId);

                    if (te == null)
                    {
                        tx.Commit();
                        timeoutData = null;
                        return false;
                    }

                    timeoutData = new TimeoutData
                    {
                        Destination = te.Destination,
                        Id = te.Id.ToString(),
                        SagaId = te.SagaId,
                        State = te.State,
                        Time = te.Time,
                        Headers = ConvertStringToDictionary(te.Headers),
                    };

                    var queryString = string.Format("delete {0} where Id = :id", typeof(TimeoutEntity));

                    found = session.CreateQuery(queryString)
                        .SetParameter("id", timeoutId)
                        .ExecuteUpdate() > 0;

                    tx.Commit();
                }
            }

            if (!found)
            {
                timeoutData = null;
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Removes the time by saga id.
        /// </summary>
        /// <param name="sagaId">The saga id of the timeouts to remove.</param>
        public void RemoveTimeoutBy(Guid sagaId)
        {
            IDbConnection connection;

            if (TryGetConnection(out connection))
            {
                RemoveTimeoutEntityBySagaId(sagaId, connection);
            }
            else
            {
                using (connection = SessionFactory.GetConnection())
                {
                    RemoveTimeoutEntityBySagaId(sagaId, connection);
                }
            }
        }

        bool TryGetConnection(out IDbConnection connection)
        {
            return DbConnectionProvider.TryGetConnection(out connection, ConnectionString);
        }

        void RemoveTimeoutEntityBySagaId(Guid sagaId, IDbConnection connection)
        {
            using (var session = SessionFactory.OpenStatelessSessionEx(connection))
            {
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
        }

        static Guid GenerateCombGuid()
        {
            var guidArray = Guid.NewGuid().ToByteArray();

            var baseDate = new DateTime(1900, 1, 1);
            var now = DateTime.Now;

            // Get the days and milliseconds which will be used to build the byte string 
            var days = new TimeSpan(now.Ticks - baseDate.Ticks);
            var timeOfDay = now.TimeOfDay;

            // Convert to a byte array 
            // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333 
            var daysArray = BitConverter.GetBytes(days.Days);
            var millisecondArray = BitConverter.GetBytes((long)(timeOfDay.TotalMilliseconds / 3.333333));

            // Reverse the bytes to match SQL Servers ordering 
            Array.Reverse(daysArray);
            Array.Reverse(millisecondArray);

            // Copy the bytes into the guid 
            Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
            Array.Copy(millisecondArray, millisecondArray.Length - 4, guidArray, guidArray.Length - 4, 4);

            return new Guid(guidArray);
        }

        static Dictionary<string, string> ConvertStringToDictionary(string data)
        {
            if (String.IsNullOrEmpty(data))
            {
                return new Dictionary<string, string>();
            }

            return (Dictionary<string, string>) serializer.DeserializeObject(data, typeof(Dictionary<string, string>));
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