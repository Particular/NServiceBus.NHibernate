namespace Runner
{
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;

    class TransportConfigOverride : IProvideConfiguration<TransportConfig>
    {
        public TransportConfig GetConfiguration()
        {
            return new TransportConfig()
            {
                MaxRetries = 10
            };
        }
    }
}