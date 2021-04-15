using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;

public partial class ConfigureEndpointSqlServerTransport
{
    class TestingSqlTransport : SqlServerTransport
    {
        public TestingSqlTransport(string connectionString)
            : base(connectionString)
        {
        }

        public string[] ReceiveAddresses { get; set; }

        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
        {
            ReceiveAddresses = receivers.Select(r => r.ReceiveAddress).ToArray();
            return base.Initialize(hostSettings, receivers, sendingAddresses, cancellationToken);
        }

    }
}