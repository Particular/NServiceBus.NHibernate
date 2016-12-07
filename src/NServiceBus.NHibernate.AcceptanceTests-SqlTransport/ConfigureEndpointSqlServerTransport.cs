using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;
using NServiceBus.Persistence;

public class ConfigureScenariosForSqlServerTransport : IConfigureSupportedScenariosForTestExecution
{
    public IEnumerable<Type> UnsupportedScenarioDescriptorTypes { get; } = new[]
    {
        typeof(AllTransportsWithCentralizedPubSubSupport)
    };
}
public class ConfigureEndpointSqlServerTransport : EndpointConfigurer
{
    public override Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata metadata)
    {
        configuration.UseTransport<SqlServerTransport>()
            .ConnectionString(ConnectionString);
        configuration.UsePersistence<NHibernatePersistence>()
            .ConnectionString(ConnectionString);

        return Task.FromResult(0);
    }
}