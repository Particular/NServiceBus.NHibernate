namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using Infrastructure;
    using NUnit.Framework;
    using Persistence;
    using Persistence.NHibernate;

    public class TransactionSessionDefaultServer : DefaultServer
    {
        public override async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration,
            Action<EndpointConfiguration> configurationBuilderCustomization) =>
            await base.GetConfiguration(runDescriptor, endpointConfiguration, configuration =>
        {
            NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
            {
                {"NServiceBus/Persistence/NHibernate/show_sql", "true"}
            };

            var builder = new EndpointConfiguration(endpointConfiguration.EndpointName);
            builder.EnableInstallers();

            builder.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0))
                .Immediate(immediate => immediate.NumberOfRetries(0));
            builder.SendFailedMessagesTo("error");

            var storageDir = Path.Combine(Path.GetTempPath(), "learn", TestContext.CurrentContext.Test.ID);

            var transport = builder.UseTransport<AcceptanceTestingTransport>();
            transport.StorageDirectory(storageDir);

            var persistence = builder.UsePersistence<NHibernatePersistence>();
            persistence.ConnectionString(ConnectionString);
            persistence.EnableTransactionalSession();

            configuration.EnableFeature<CaptureBuilderFeature>();

            configurationBuilderCustomization(configuration);
        }).ConfigureAwait(false);

        const string DefaultConnStr = @"Server=localhost\SqlExpress;Database=nservicebus;Trusted_Connection=True;";

        public static string ConnectionString
        {
            get
            {
                var env = Environment.GetEnvironmentVariable("SQLServerConnectionString");
                return string.IsNullOrEmpty(env) ? DefaultConnStr : env;
            }
        }
    }
}