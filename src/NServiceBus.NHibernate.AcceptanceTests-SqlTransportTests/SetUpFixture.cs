using System;
using NServiceBus;
using NUnit.Framework;

[SetUpFixture]
public class SetUpFixture
{
    [OneTimeSetUp]
    public void SetUp()
    {
        var connectionString = Environment.GetEnvironmentVariable("SQLServerConnectionString");
        var ci = Environment.GetEnvironmentVariable("CI");
        if (string.IsNullOrWhiteSpace(connectionString) && ci == "true")
        {
            Assert.Ignore("Ignoring SQLServer test");
        }

        // ensure the NHibernate persistence assembly is loaded into the AppDomain because it needs its features to be scanned to work properly.
        typeof(NHibernatePersistence).ToString();
    }
}