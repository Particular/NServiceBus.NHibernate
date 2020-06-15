using System.Collections.Specialized;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence;
using NServiceBus.Persistence.NHibernate;

public class ConfigureEndpointNHibernatePersistence : EndpointConfigurer
{
    public override Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
        {
            {"NServiceBus/Persistence/NHibernate/show_sql", "true"}
        };

        configuration.UsePersistence<NHibernatePersistence>()
            .ConnectionString(ConnectionString);

        return Task.FromResult(0);
    }
}