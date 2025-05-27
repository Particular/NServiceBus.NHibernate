namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using AcceptanceTesting.Support;
using global::NHibernate.Driver;
using NServiceBus.Configuration.AdvancedExtensibility;
using NUnit.Framework;
using Persistence.NHibernate;
using Persistence;

public class DefaultServer : IEndpointSetupTemplate
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

    public virtual async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomization,
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

        var endpointConfiguration = new EndpointConfiguration(endpointCustomization.EndpointName);

        endpointConfiguration.EnableInstallers();
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.Recoverability()
            .Delayed(delayed => delayed.NumberOfRetries(0))
            .Immediate(immediate => immediate.NumberOfRetries(0));
        endpointConfiguration.SendFailedMessagesTo("error");

        var storageDir = Path.Combine(Path.GetTempPath(), "learn", TestContext.CurrentContext.Test.ID);

        endpointConfiguration.UseTransport(new AcceptanceTestingTransport { StorageLocation = storageDir });

        var persistence = endpointConfiguration.UsePersistence<NHibernatePersistence>();
        persistence.ConnectionString(ConnectionString);

        endpointConfiguration.GetSettings().Set(persistence);

        if (runDescriptor.ScenarioContext is TransactionalSessionTestContext testContext)
        {
            endpointConfiguration.RegisterStartupTask(sp => new CaptureServiceProviderStartupTask(sp, testContext, endpointCustomization.EndpointName));
        }

        await configurationBuilderCustomization(endpointConfiguration).ConfigureAwait(false);

        // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
        endpointConfiguration.TypesToIncludeInScan(endpointCustomization.GetTypesScopedByTestClass());

        return endpointConfiguration;
    }
}