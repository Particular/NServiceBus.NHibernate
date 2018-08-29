namespace NServiceBus.Deduplication.NHibernate
{
    using System;
    using System.Data;
    using Config;
    using Gateway.Deduplication;
    using global::NHibernate;
    using global::NHibernate.Exceptions;
    using Persistence.NHibernate;

    class GatewayDeduplication : IDeduplicateMessages
    {
        public ISessionFactory SessionFactory { get; set; }

        public bool DeduplicateMessage(string clientId, DateTime timeReceived)
        {
            using (var conn = SessionFactory.GetConnection())
            using (var session = SessionFactory.OpenSessionEx(conn))
            using (var tx = session.BeginAmbientTransactionAware(IsolationLevel.ReadCommitted))
            {
                var gatewayMessage = session.Get<DeduplicationMessage>(clientId);

                if (gatewayMessage != null)
                {
                    tx.Commit();
                    return false;
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
