namespace NServiceBus.AcceptanceTests
{
    public partial class NServiceBusAcceptanceTest
    {
        static NServiceBusAcceptanceTest()
        {
            // Hack: prevents SerializationException ... Type 'x' in assembly 'y' is not marked as serializable.
            // https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/mitigation-deserialization-of-objects-across-app-domains
            System.Configuration.ConfigurationManager.GetSection("X");
        }
    }
}