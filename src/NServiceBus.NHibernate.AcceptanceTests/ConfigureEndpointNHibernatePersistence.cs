using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence;

public class ConfigureScenariosForNHibernatePersistence : IConfigureSupportedScenariosForTestExecution
{
    public IEnumerable<Type> UnsupportedScenarioDescriptorTypes { get; } = new List<Type>();
}

public class ConfigureEndpointNHibernatePersistence : IConfigureEndpointTestExecution
{
    public static string ConnectionString
    {
        get
        {
            var envVar = Environment.GetEnvironmentVariable("NH_ACC_TEST_CONNSTR");
            if (!string.IsNullOrEmpty(envVar))
            {
                return envVar;
            }

            return defaultConnStr;
        }
    }

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
    {
        configuration.UsePersistence<NHibernatePersistence>()
            .ConnectionString(ConnectionString);

        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        return Task.FromResult(0);
    }

    const string defaultConnStr = @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;";
}