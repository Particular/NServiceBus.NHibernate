namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using AcceptanceTesting.Support;
using global::NHibernate.Driver;
using NUnit.Framework;
using Persistence.NHibernate;

public class DefaultServer : IEndpointSetupTemplate
{
    public virtual async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor,
        EndpointCustomizationConfiguration endpointConfiguration,
        Func<EndpointConfiguration, Task> configurationBuilderCustomization)
    {
        NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
        {
            { "NServiceBus/Persistence/NHibernate/show_sql", "true" },
            {
                "NServiceBus/Persistence/NHibernate/connection.driver_class",
                typeof(MicrosoftDataSqlClientDriver).FullName
            }
        };

        var builder = new EndpointConfiguration(endpointConfiguration.EndpointName);
        builder.UseSerialization<SystemJsonSerializer>();
        builder.EnableInstallers();

        builder.Recoverability()
            .Delayed(delayed => delayed.NumberOfRetries(0))
            .Immediate(immediate => immediate.NumberOfRetries(0));
        builder.SendFailedMessagesTo("error");

        string storageDir = Path.Combine(Path.GetTempPath(), "learn", TestContext.CurrentContext.Test.ID);

        builder.UseTransport(new AcceptanceTestingTransport { StorageLocation = storageDir });

        if (runDescriptor.ScenarioContext is TransactionalSessionTestContext testContext)
        {
            builder.RegisterStartupTask(sp => new CaptureServiceProviderStartupTask(sp, testContext, endpointConfiguration.EndpointName));
        }

        await configurationBuilderCustomization(builder).ConfigureAwait(false);

        // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
        builder.TypesToIncludeInScan(endpointConfiguration.GetTypesScopedByTestClass());

        return builder;
    }
}