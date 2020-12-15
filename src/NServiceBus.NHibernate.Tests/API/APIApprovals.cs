namespace NServiceBus.NHibernate.Tests.API
{
    using NUnit.Framework;
    using Particular.Approvals;
    using PublicApiGenerator;

    [TestFixture]
    public class APIApprovals
    {
        [Test]
        public void Approve()
        {
            var publicApi = typeof(NHibernatePersistence).Assembly.GeneratePublicApi(new ApiGeneratorOptions
            {
                ExcludeAttributes = new[] { "System.Runtime.Versioning.TargetFrameworkAttribute", "System.Reflection.AssemblyMetadataAttribute" }
            });
            Approver.Verify(publicApi);
        }
    }
}