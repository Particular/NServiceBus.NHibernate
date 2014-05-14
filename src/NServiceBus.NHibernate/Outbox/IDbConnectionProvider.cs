namespace NServiceBus.Outbox
{
    using System.Data;

    interface IDbConnectionProvider
    {
        IDbConnection Connection { get; }
    }
}