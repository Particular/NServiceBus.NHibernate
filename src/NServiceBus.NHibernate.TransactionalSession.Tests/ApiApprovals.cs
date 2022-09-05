namespace NServiceBus.Persistence.CosmosDB.Tests
{
    using NUnit.Framework;
    using Particular.Approvals;
    using PublicApiGenerator;
    using TransactionalSession;

    [TestFixture]
    public class APIApprovals
    {
        [Test]
        public void Approve()
        {
            var publicApi = typeof(NHibernateOpenSessionOptions).Assembly.GeneratePublicApi(new ApiGeneratorOptions
            {
                ExcludeAttributes = new[] { "System.Runtime.Versioning.TargetFrameworkAttribute", "System.Reflection.AssemblyMetadataAttribute" }
            });
            Approver.Verify(publicApi);
        }
    }
}
