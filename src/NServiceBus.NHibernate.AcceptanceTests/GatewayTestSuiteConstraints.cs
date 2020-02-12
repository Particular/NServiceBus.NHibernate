namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using Persistence;

    public partial class GatewayTestSuiteConstraints
    {
        public Task ConfigureDeduplicationStorage(string endpointName, EndpointConfiguration configuration, RunSettings settings)
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