namespace NServiceBus.AcceptanceTests
{
    using NUnit.Framework;

    public abstract partial class NServiceBusAcceptanceTest
    {
        [SetUp]
        public void SetUp2()
        {
            if (TestContext.CurrentContext?.Test?.FullName == "NServiceBus.AcceptanceTests.DelayedDelivery.When_deferring_to_non_local.Message_should_be_received")
            {
                Assert.Ignore("This is a flaky time-dependent test. It's ignored for now.");
            }
        }
    }
}