namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.Threading.Tasks;
using AcceptanceTesting.Support;
using Configuration.AdvancedExtensibility;

public class TransactionSessionDefaultServer : DefaultServer
{
    public override async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration,
        Func<EndpointConfiguration, Task> configurationBuilderCustomization) =>
        await base.GetConfiguration(runDescriptor, endpointConfiguration, async configuration =>
        {
            configuration.GetSettings().Get<PersistenceExtensions<NHibernatePersistence>>().EnableTransactionalSession();

            await configurationBuilderCustomization(configuration);
        });
}