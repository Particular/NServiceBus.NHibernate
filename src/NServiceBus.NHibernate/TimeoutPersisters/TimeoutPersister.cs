namespace NServiceBus.TimeoutPersisters.NHibernate
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using global::NHibernate;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.NHibernate;
    using Timeout.Core;
    using IsolationLevel = System.Data.IsolationLevel;

    class TimeoutPersister : IPersistTimeouts, IQueryTimeouts
    {
        ISessionFactory SessionFactory;
        string EndpointName;

        public TimeoutPersister(string endpointName, ISessionFactory sessionFactory)
        {
            EndpointName = endpointName;
            SessionFactory = sessionFactory;
        }

        public Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var now = DateTime.UtcNow;
                using (var session = SessionFactory.OpenStatelessSession())
                using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    var results = session.QueryOver<TimeoutEntity>()
                        .Where(x => x.Endpoint == EndpointName)
                        .And(x => x.Time >= startSlice && x.Time <= now)
                        .OrderBy(x => x.Time).Asc
                        .Select(x => x.Id, x => x.Time)
                        .List<object[]>()
                        .Select(p =>
                        {
                            var id = (Guid) p[0];
                            var dueTime = (DateTime) p[1];
                            return new TimeoutsChunk.Timeout(id.ToString(), dueTime);
                        })
                        .ToList();

                    //Retrieve next time we need to run query
                    var startOfNextChunk = session.QueryOver<TimeoutEntity>()
                        .Where(x => x.Endpoint == EndpointName)
                        .Where(x => x.Time > now)
                        .OrderBy(x => x.Time).Asc
                        .Take(1)
                        .SingleOrDefault();

                    var nextTimeToRunQuery = startOfNextChunk?.Time ?? DateTime.UtcNow.AddMinutes(10);

                    tx.Commit();
                    return Task.FromResult(new TimeoutsChunk(results, nextTimeToRunQuery));
                }
            }
        }

        public Task Add(TimeoutData timeout, ContextBag context)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var timeoutId = Guid.NewGuid();

                using (var session = SessionFactory.OpenSession())
                using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    session.Save(new TimeoutEntity
                    {
                        Id = timeoutId,
                        Destination = timeout.Destination,
                        SagaId = timeout.SagaId,
                        State = timeout.State,
                        Time = timeout.Time,
                        Headers = ConvertDictionaryToString(timeout.Headers),
                        Endpoint = timeout.OwningTimeoutManager
                    });
                    tx.Commit();
                }

                timeout.Id = timeoutId.ToString();
                return Task.FromResult(0);
            }
        }

        public Task<bool> TryRemove(string timeoutId, ContextBag context)
        {
            var id = Guid.Parse(timeoutId);
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                using (var session = SessionFactory.OpenStatelessSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        var queryString = $"delete {typeof(TimeoutEntity)} where Id = :id";

                        var found = session.CreateQuery(queryString)
                            .SetParameter("id", id)
                            .ExecuteUpdate() > 0;

                        tx.Commit();

                        return Task.FromResult(found);
                    }
                }
            }
        }

        public Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                using (var session = SessionFactory.OpenStatelessSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        var queryString = $"delete {typeof(TimeoutEntity)} where SagaId = :sagaid";
                        session.CreateQuery(queryString)
                            .SetParameter("sagaid", sagaId)
                            .ExecuteUpdate();

                        tx.Commit();

                        return Task.FromResult(0);
                    }
                }

            }
        }

        public Task<TimeoutData> Peek(string timeoutId, ContextBag context)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var id = Guid.Parse(timeoutId);
                using (var session = SessionFactory.OpenStatelessSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        var te = session.QueryOver<TimeoutEntity>()
                            .Where(x => x.Id == id)
                            .List()
                            .SingleOrDefault();

                        var timeout = MapToTimeoutData(te);

                        tx.Commit();
                        return Task.FromResult(timeout);
                    }
                }
            }
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

        static Dictionary<string, string> ConvertStringToDictionary(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return new Dictionary<string, string>();
            }

            return ObjectSerializer.DeSerialize<Dictionary<string, string>>(data);
        }

        static string ConvertDictionaryToString(ICollection data)
        {
            if (data == null || data.Count == 0)
            {
                return null;
            }

            return ObjectSerializer.Serialize(data);
        }

    }
}