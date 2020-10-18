namespace NServiceBus.TimeoutPersisters.NHibernate
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlTypes;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using global::NHibernate;
    using Extensibility;
    using Persistence;
    using Persistence.NHibernate;
    using Transport;
    using Timeout.Core;
    using IsolationLevel = System.Data.IsolationLevel;

    class TimeoutPersister : IPersistTimeouts, IQueryTimeouts
    {
        ISessionFactory SessionFactory;
        ISynchronizedStorageAdapter transportTransactionAdapter;
        ISynchronizedStorage synchronizedStorage;
        TimeSpan timeoutsCleanupExecutionInterval;
        string EndpointName;

        DateTimeOffset lastTimeoutsCleanupExecution = DateTimeOffset.MinValue;

        public TimeoutPersister(string endpointName,
            ISessionFactory sessionFactory,
            ISynchronizedStorageAdapter transportTransactionAdapter,
            ISynchronizedStorage synchronizedStorage,
            TimeSpan timeoutsCleanupExecutionInterval)
        {
            EndpointName = endpointName;
            SessionFactory = sessionFactory;
            this.transportTransactionAdapter = transportTransactionAdapter;
            this.synchronizedStorage = synchronizedStorage;
            this.timeoutsCleanupExecutionInterval = timeoutsCleanupExecutionInterval;
        }

        public async Task<TimeoutsChunk> GetNextChunk(DateTimeOffset startSlice)
        {
            var now = DateTimeOffset.UtcNow;

            //Every timeoutsCleanupExecutionInterval we extend the query window back in time to make
            //sure we will pick-up any missed timeouts which might exists due to TimeoutManager timeout storage race-condition
            if (lastTimeoutsCleanupExecution.Add(timeoutsCleanupExecutionInterval) < now)
            {
                lastTimeoutsCleanupExecution = now;
                //We cannot use DateTime.MinValue as sql supports dates only back to 1 January 1753
                startSlice = SqlDateTime.MinValue.Value;
            }

            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            using (var session = SessionFactory.OpenStatelessSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var list = await session.QueryOver<TimeoutEntity>()
                    .Where(x => x.Endpoint == EndpointName)
                    .And(x => x.Time > startSlice && x.Time <= now)
                    .OrderBy(x => x.Time).Asc
                    .Select(x => x.Id, x => x.Time)
                    .ListAsync<object[]>().ConfigureAwait(false);

                var results = list
                    .Select(p =>
                    {
                        var id = (Guid)p[0];
                        var dueTime = (DateTime)p[1];
                        return new TimeoutsChunk.Timeout(id.ToString(), dueTime);
                    })
                    .ToArray();

                //Retrieve next time we need to run query
                var startOfNextChunk = await session.QueryOver<TimeoutEntity>()
                    .Where(x => x.Endpoint == EndpointName && x.Time > now)
                    .OrderBy(x => x.Time).Asc
                    .Select(x => x.Time)
                    .Take(1)
                    .SingleOrDefaultAsync<DateTime?>().ConfigureAwait(false);

                var nextTimeToRunQuery = startOfNextChunk ?? DateTime.UtcNow.AddMinutes(10);

                await tx.CommitAsync()
                    .ConfigureAwait(false);
                return new TimeoutsChunk(results, nextTimeToRunQuery);
            }
        }

        public async Task Add(TimeoutData timeout, ContextBag context)
        {
            using (var session = await OpenSession(context).ConfigureAwait(false))
            {
                var timeoutEntity = new TimeoutEntity
                {
                    Destination = timeout.Destination,
                    SagaId = timeout.SagaId,
                    State = timeout.State,
                    Time = timeout.Time.DateTime,
                    Headers = ConvertDictionaryToString(timeout.Headers),
                    Endpoint = timeout.OwningTimeoutManager
                };

                var timeoutId = (Guid)await session.Session().SaveAsync(timeoutEntity)
                    .ConfigureAwait(false);
                await session.CompleteAsync()
                    .ConfigureAwait(false);

                timeout.Id = timeoutId.ToString();
            }
        }

        public async Task<bool> TryRemove(string timeoutId, ContextBag context)
        {
            var id = Guid.Parse(timeoutId);
            using (var session = await OpenSession(context).ConfigureAwait(false))
            {
                var queryString = $"delete {typeof(TimeoutEntity)} where Id = :id";
                var found = await session.Session().CreateQuery(queryString).SetParameter("id", id).ExecuteUpdateAsync().ConfigureAwait(false) > 0;
                await session.CompleteAsync().ConfigureAwait(false);
                return found;
            }
        }

        async Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag context)
        {
            var transportTransaction = context.GetOrCreate<TransportTransaction>();
            var session = await transportTransactionAdapter.TryAdapt(transportTransaction, context).ConfigureAwait(false)
                ?? await synchronizedStorage.OpenSession(context).ConfigureAwait(false);
            return session;
        }

        public async Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            using (var session = SessionFactory.OpenStatelessSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var queryString = $"delete {typeof(TimeoutEntity)} where SagaId = :sagaid";
                await session.CreateQuery(queryString)
                    .SetParameter("sagaid", sagaId)
                    .ExecuteUpdateAsync()
                    .ConfigureAwait(false);

                await tx.CommitAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task<TimeoutData> Peek(string timeoutId, ContextBag context)
        {
            using (var session = await OpenSession(context).ConfigureAwait(false))
            {
                var id = Guid.Parse(timeoutId);
                var te = await session.Session().GetAsync<TimeoutEntity>(id, LockMode.Upgrade).ConfigureAwait(false);

                return MapToTimeoutData(te);
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

        static string ConvertDictionaryToString(Dictionary<string, string> data)
        {
            if (data == null || data.Count == 0)
            {
                return null;
            }

            return ObjectSerializer.Serialize(data);
        }
    }
}