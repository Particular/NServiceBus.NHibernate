namespace NServiceBus.Outbox
{
    using System.Data;

    interface IDbConnectionProvider
    {
        bool TryGetConnection(out IDbConnection connection, string connectionString);
    }
}