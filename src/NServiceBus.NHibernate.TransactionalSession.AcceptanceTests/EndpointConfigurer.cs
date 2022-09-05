using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public abstract class EndpointConfigurer : IConfigureEndpointTestExecution
{
    const string defaultConnStr = @"Server=localhost\SqlExpress;Database=nservicebus;Trusted_Connection=True;";

    public static string ConnectionString
    {
        get
        {
            var env = Environment.GetEnvironmentVariable("SQLServerConnectionString");
            return string.IsNullOrEmpty(env) ? defaultConnStr : env;
        }
    }

    public abstract Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata);

    public Task Cleanup()
    {
        return Task.FromResult(0);
    }
}