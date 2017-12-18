namespace NServiceBus.Gateway.AcceptanceTests
{
    public partial class GatewayTestSuiteConstraints : IGatewayTestSuiteConstraints
    {
        public IConfigureGatewayPersitenceExecution CreatePersistenceConfiguration()
        {
            return new ConfigureNHibernateGatewayPersistenceExecution();
        }
    }
}