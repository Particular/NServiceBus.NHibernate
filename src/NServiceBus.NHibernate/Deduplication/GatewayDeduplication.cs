namespace NServiceBus.Deduplication.NHibernate
{
    using System;
    using System.Data;
    using System.Threading.Tasks;
    using Config;
    using Gateway.Deduplication;
    using global::NHibernate;
    using global::NHibernate.Exceptions;
    using Extensibility;

    class GatewayDeduplication : IDeduplicateMessages
    {
        readonly ISessionFactory sessionFactory;

        public GatewayDeduplication(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public Task<bool> DeduplicateMessage(string clientId, DateTime timeReceived, ContextBag context)
        {
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var gatewayMessage = session.Get<DeduplicationMessage>(clientId);

                if (gatewayMessage != null)
                {
                    tx.Commit();
                    return Task.FromResult(false);
                }

                gatewayMessage = new DeduplicationMessage
                {
                    Id = clientId,
                    TimeReceived = timeReceived
                };

                try
                {
                    session.Save(gatewayMessage);
                    tx.Commit();
                }
                catch (ConstraintViolationException)
                {
                    tx.Rollback();
                    return Task.FromResult(false);
                }
                catch (ADOException)
                {
                    tx.Rollback();
                    throw;
                }
            }

            return Task.FromResult(true);
        }
    }
}
