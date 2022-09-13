namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using Infrastructure;
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

            var persistence = configuration.UsePersistence<NHibernatePersistence>();
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