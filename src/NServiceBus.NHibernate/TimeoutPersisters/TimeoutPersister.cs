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

    class TimeoutPersister : IPersistTimeouts, IPersistTimeoutsV2
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
                            .Select(x => x.Time)
                            .Take(1)
                            .SingleOrDefault<DateTime?>();

                        if (startOfNextChunk != null)
                        {
                            nextTimeToRunQuery = startOfNextChunk.Value;
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
            Guid timeoutId;
            IDbConnection connection;

            if (TryGetConnection(out connection))
            {
                timeoutId = StoreTimeoutEntity(timeout, connection);
            }
            else
            {
                using (connection = SessionFactory.GetConnection())
                {
                    timeoutId = StoreTimeoutEntity(timeout, connection);
                }
            }

            timeout.Id = timeoutId.ToString();
        }

        Guid StoreTimeoutEntity(TimeoutData timeout, IDbConnection connection)
        {
            using (var session = SessionFactory.OpenSessionEx(connection))
            {
                using (var tx = session.BeginAmbientTransactionAware(IsolationLevel.ReadCommitted))
                {
                    var entity = new TimeoutEntity
                    {
                        Destination = timeout.Destination,
                        SagaId = timeout.SagaId,
                        State = timeout.State,
                        Time = timeout.Time,
                        Headers = ConvertDictionaryToString(timeout.Headers),
                        Endpoint = timeout.OwningTimeoutManager,
                    };

                    var id = (Guid)session.Save(entity);

                    tx.Commit();

                    return id;
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
            using (var session = SessionFactory.OpenStatelessSessionEx(connection))
            {
                using (var tx = session.BeginAmbientTransactionAware(IsolationLevel.ReadCommitted))
                {
                    var te = session.Get<TimeoutEntity>(timeoutId, LockMode.Upgrade);
                    timeoutData = MapToTimeoutData(te);

                    if (timeoutData == null)
                    {
                        tx.Commit();
                        return false;
                    }


                    session.Delete(te);
                    tx.Commit();
                }
            }

            return true;
        }

        public bool TryRemove(string timeoutId)
        {
            IDbConnection connection;

            if (TryGetConnection(out connection))
            {
                return TryRemoveTimeoutEntity(Guid.Parse(timeoutId), connection);
            }

            using (connection = SessionFactory.GetConnection())
            {
                return TryRemoveTimeoutEntity(Guid.Parse(timeoutId), connection);
            }
        }

        bool TryRemoveTimeoutEntity(Guid timeoutId, IDbConnection connection)
        {
            using (var session = SessionFactory.OpenStatelessSessionEx(connection))
            {
                using (var tx = session.BeginAmbientTransactionAware(IsolationLevel.ReadCommitted))
                {
                    var queryString = string.Format("delete {0} where Id = :id", typeof(TimeoutEntity));

                    var found = session.CreateQuery(queryString)
                        .SetParameter("id", timeoutId)
                        .ExecuteUpdate() > 0;

                    tx.Commit();

                    return found;
                }
            }
        }

        public TimeoutData Peek(string timeoutId)
        {
            IDbConnection connection;

            if (TryGetConnection(out connection))
            {
                return TryGetTimeout(Guid.Parse(timeoutId), connection);
            }

            using (connection = SessionFactory.GetConnection())
            {
                return TryGetTimeout(Guid.Parse(timeoutId), connection);
            }
        }

        TimeoutData TryGetTimeout(Guid timeoutId, IDbConnection connection)
        {
            using (var session = SessionFactory.OpenStatelessSessionEx(connection))
            {
                using (var tx = session.BeginAmbientTransactionAware(IsolationLevel.ReadCommitted))
                {
                    var te = session.Get<TimeoutEntity>(timeoutId, LockMode.Upgrade);
                    var timeout = MapToTimeoutData(te);

                    tx.Commit();
                    return timeout;
                }
            }
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

        static TimeoutData MapToTimeoutData(TimeoutEntity te)
        {
            if (te == null)
            {
                return null;
            }

            return new TimeoutData
            {
                Destination = te.Destination,
                Id = te.Id.ToString(),
                SagaId = te.SagaId,
                State = te.State,
                Time = te.Time,
                Headers = ConvertStringToDictionary(te.Headers),
            };
        }

        void RemoveTimeoutEntityBySagaId(Guid sagaId, IDbConnection connection)
        {
            using (var session = SessionFactory.OpenStatelessSessionEx(connection))
            {
                using (var tx = session.BeginAmbientTransactionAware(IsolationLevel.ReadCommitted))
                {
                    var queryString = $"delete {typeof(TimeoutEntity)} where SagaId = :sagaid";
                    session.CreateQuery(queryString)
                        .SetParameter("sagaid", sagaId)
                        .ExecuteUpdate();

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
