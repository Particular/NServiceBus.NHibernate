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

public abstract class EndpointConfigurer : IConfigureEndpointTestExecution
{
    const string defaultConnStr = @"Server=localhost\SqlExpress;Database=nservicebus;Trusted_Connection=True;";

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

    public abstract Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings);

    public Task Cleanup()
    {
        return Task.FromResult(0);
    }
}

public class ConfigureEndpointSqlServerTransport : EndpointConfigurer
{
    public override Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
    {
        configuration.UseTransport<SqlServerTransport>()
            .ConnectionString(ConnectionString);
        configuration.UsePersistence<NHibernatePersistence>()
            .ConnectionString(ConnectionString);

        return Task.FromResult(0);
    }
}

public class ConfigureEndpointNHibernatePersistence : EndpointConfigurer
{
    public override Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
    {
        configuration.UsePersistence<NHibernatePersistence>()
            .ConnectionString(ConnectionString);

        return Task.FromResult(0);
    }
}