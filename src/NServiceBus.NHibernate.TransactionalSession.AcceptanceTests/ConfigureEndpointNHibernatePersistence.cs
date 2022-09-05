using System.Collections.Specialized;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence;
using NServiceBus.Persistence.NHibernate;
using NServiceBus.TransactionalSession;

public class ConfigureEndpointNHibernatePersistence : EndpointConfigurer
{
    public override Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
        {
            {"NServiceBus/Persistence/NHibernate/show_sql", "true"}
        };

        var persistence = configuration.UsePersistence<NHibernatePersistence>();
        persistence.ConnectionString(ConnectionString);
        persistence.EnableTransactionalSession();

        return Task.FromResult(0);
    }
}