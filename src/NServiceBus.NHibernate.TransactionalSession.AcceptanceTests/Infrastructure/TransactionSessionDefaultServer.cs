namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.Threading.Tasks;
using AcceptanceTesting.Support;

public class TransactionSessionDefaultServer : DefaultServer
{
    public override async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration,
        Func<EndpointConfiguration, Task> configurationBuilderCustomization) =>
        await base.GetConfiguration(runDescriptor, endpointConfiguration, async configuration =>
        {
            PersistenceExtensions<NHibernatePersistence> persistence = configuration.UsePersistence<NHibernatePersistence>();
            persistence.EnableTransactionalSession();

            await configurationBuilderCustomization(configuration);
        });
}