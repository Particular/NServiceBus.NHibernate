using System;
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
    }
}
