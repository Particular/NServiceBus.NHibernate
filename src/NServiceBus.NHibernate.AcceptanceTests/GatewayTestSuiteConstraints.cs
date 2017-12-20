namespace NServiceBus.Gateway.AcceptanceTests
{
    public partial class GatewayTestSuiteConstraints
    {
        public IConfigureGatewayPersitenceExecution CreatePersistenceConfiguration()
        {
            return new ConfigureNHibernateGatewayPersistenceExecution();
        }
    }
}