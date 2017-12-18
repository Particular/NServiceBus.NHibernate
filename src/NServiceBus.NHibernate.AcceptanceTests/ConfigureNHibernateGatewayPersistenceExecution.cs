namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using Persistence;

    class ConfigureNHibernateGatewayPersistenceExecution : IConfigureGatewayPersitenceExecution
    {
        public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
        {
            configuration.UsePersistence<NHibernatePersistence, StorageType.GatewayDeduplication>()
                .ConnectionString(EndpointConfigurer.ConnectionString);

            return Task.FromResult(0);
        }

        public Task Cleanup()
        {
            return Task.FromResult(0);
        }
    }
}