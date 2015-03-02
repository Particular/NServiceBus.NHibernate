namespace NServiceBus.AcceptanceTests
{
    using System;
    using NUnit.Framework;

    public abstract partial class NServiceBusAcceptanceTest
    {
        [SetUp]
        public void SetUpTransport()
        {
            Environment.SetEnvironmentVariable("Transport.UseSpecific", "MsmqTransport");
        }
    }
}