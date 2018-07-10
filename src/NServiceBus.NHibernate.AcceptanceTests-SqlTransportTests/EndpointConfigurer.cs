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
            var env = Environment.GetEnvironmentVariable("NH_ACC_TEST_CONNSTR");
            if (!string.IsNullOrEmpty(env))
            {
                return env;
            }

            env = Environment.GetEnvironmentVariable("SQLServerConnectionString");
            if (!string.IsNullOrEmpty(env))
            {
                return env;
            }


            return defaultConnStr;
        }
    }

    public abstract Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata);

    public virtual Task Cleanup()
    {
        return Task.FromResult(0);
    }
}