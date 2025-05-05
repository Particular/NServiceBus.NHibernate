namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using AcceptanceTesting.Support;
using Configuration.AdvancedExtensibility;
using global::NHibernate.Driver;
using NUnit.Framework;
using Persistence;
using Persistence.NHibernate;

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

        PersistenceExtensions<NHibernatePersistence> persistence = builder.UsePersistence<NHibernatePersistence>();
        persistence.ConnectionString(ConnectionString);
        persistence.EnableTransactionalSession();

        builder.GetSettings().Set(persistence);

        if (!typeof(IDoNotCaptureServiceProvider).IsAssignableFrom(endpointConfiguration.BuilderType))
        {
            builder.RegisterStartupTask(sp => new CaptureServiceProviderStartupTask(sp, runDescriptor.ScenarioContext));
        }

        await configurationBuilderCustomization(builder).ConfigureAwait(false);

        // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
        builder.TypesToIncludeInScan(endpointConfiguration.GetTypesScopedByTestClass());

        return builder;
    }
}