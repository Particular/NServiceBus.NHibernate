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

        public async Task<bool> DeduplicateMessage(string clientId, DateTime timeReceived, ContextBag context)
        {
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var gatewayMessage = await session.GetAsync<DeduplicationMessage>(clientId)
                    .ConfigureAwait(false);

                if (gatewayMessage != null)
                {
                    await tx.CommitAsync()
                        .ConfigureAwait(false);
                    return false;
                }

                gatewayMessage = new DeduplicationMessage
                {
                    Id = clientId,
                    TimeReceived = timeReceived
                };

                try
                {
                    await session.SaveAsync(gatewayMessage)
                        .ConfigureAwait(false);
                    await tx.CommitAsync()
                        .ConfigureAwait(false);
                }
                catch (ConstraintViolationException)
                {
                    await tx.RollbackAsync()
                        .ConfigureAwait(false);
                    return false;
                }
                catch (ADOException)
                {
                    await tx.RollbackAsync()
                        .ConfigureAwait(false);
                    throw;
                }
            }

            return true;
        }
    }
}
