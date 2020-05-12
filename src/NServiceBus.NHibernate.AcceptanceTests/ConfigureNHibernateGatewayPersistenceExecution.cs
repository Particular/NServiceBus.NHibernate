namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using Persistence;

    class ConfigureNHibernateGatewayPersistenceExecution : IConfigureGatewayPersitenceExecution
    {
        public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            configuration.UsePersistence<NHibernatePersistence, StorageType.GatewayDeduplication>()
#pragma warning restore CS0618 // Type or member is obsolete
                .ConnectionString(EndpointConfigurer.ConnectionString);

            return Task.FromResult(0);
        }

        public Task Cleanup()
        {
            return Task.FromResult(0);
        }
    }
}