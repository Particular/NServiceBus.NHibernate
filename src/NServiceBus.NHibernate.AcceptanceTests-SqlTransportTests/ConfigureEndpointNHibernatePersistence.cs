using System.Linq;
using System.Threading.Tasks;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Dialect;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence;

public class ConfigureEndpointNHibernatePersistence : EndpointConfigurer
{
    public override Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var cfg = new Configuration
        {
            Properties =
            {
                {Environment.ConnectionString, ConnectionString},
                {Environment.Dialect, typeof(MsSql2008Dialect).FullName},
            },
        };

        cfg.BeforeBindMapping += (sender, e) => PrefixMapping(e, "sqlTest_");

        configuration.UsePersistence<NHibernatePersistence>()
            .UseConfiguration(cfg);

        return Task.CompletedTask;
    }

    static void PrefixMapping(BindMappingEventArgs e, string prefix)
    {
        var c = e.Mapping.RootClasses.Single();

        c.table = prefix + (c.table ?? c.Name);
        foreach (var prop in c.Properties.OfType<HbmProperty>())
        {
            foreach (var column in prop.Columns)
            {
                if (!string.IsNullOrEmpty(column.index))
                {
                    column.index = prefix + column.index;
                }
            }
        }
    }
}