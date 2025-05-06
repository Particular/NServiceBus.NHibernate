namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.Threading.Tasks;
using AcceptanceTesting.Support;
using Persistence;

public class TransactionSessionDefaultServer : DefaultServer
{
    const string DefaultConnStr = @"Server=localhost\SqlExpress;Database=nservicebus;Trusted_Connection=True;";

    public static string ConnectionString
    {
        get
        {
            string env = Environment.GetEnvironmentVariable("SQLServerConnectionString");
            return string.IsNullOrEmpty(env) ? DefaultConnStr : env;
        }
    }
    public override async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration,
        Func<EndpointConfiguration, Task> configurationBuilderCustomization) =>
        await base.GetConfiguration(runDescriptor, endpointConfiguration, async configuration =>
        {
            PersistenceExtensions<NHibernatePersistence> persistence = configuration.UsePersistence<NHibernatePersistence>();
            persistence.ConnectionString(ConnectionString);
            persistence.EnableTransactionalSession();

            await configurationBuilderCustomization(configuration);
        });
}