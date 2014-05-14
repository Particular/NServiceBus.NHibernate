namespace NServiceBus.NHibernate.Tests.Outbox
{
    using System.Data;
    using NServiceBus.Outbox;

    class FakeDbConnectionProvider : IDbConnectionProvider
    {

        public FakeDbConnectionProvider(IDbConnection dbConnection)
        {
            Connection = dbConnection;
        }

        public IDbConnection Connection { get; private set; }
    }
}