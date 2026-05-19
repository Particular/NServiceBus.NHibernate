using System.Collections.Specialized;
using System.Threading.Tasks;
using NHibernate.Driver;
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
            { "NServiceBus/Persistence/NHibernate/show_sql", "true" },
            { "NServiceBus/Persistence/NHibernate/connection.driver_class", typeof(MicrosoftDataSqlClientDriver).FullName }
        };

        configuration.UsePersistence<NHibernatePersistence>()
            .ConnectionString(ConnectionString)
            .SagaTableNamingConvention(type =>
            {
                //if the type is nested use the name of the parent
                if (type.DeclaringType == null)
                {
                    return type.Name;
                }

                return type.DeclaringType.Name + "_" + type.Name;
            });

        return Task.FromResult(0);
    }
}