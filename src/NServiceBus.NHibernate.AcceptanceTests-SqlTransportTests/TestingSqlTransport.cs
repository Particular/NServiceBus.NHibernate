using System;
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

        public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
        {
            var infra = await base.Initialize(hostSettings, receivers, sendingAddresses, cancellationToken);

            if (infra.Receivers == null)
            {
                ReceiveAddresses = Array.Empty<string>();
            }
            else
            {
                ReceiveAddresses = infra.Receivers.Select(r => r.Value.ReceiveAddress).ToArray();
            }

            return infra;
        }
    }
}