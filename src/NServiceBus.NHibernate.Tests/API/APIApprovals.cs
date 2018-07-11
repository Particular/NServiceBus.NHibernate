namespace NServiceBus.NHibernate.Tests.API
{
    using System.Runtime.CompilerServices;
    using NUnit.Framework;
    using PublicApiGenerator;

    [TestFixture]
    public class APIApprovals
    {
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Approve()
        {
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(NHibernatePersistence).Assembly, excludeAttributes: new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" });
            TestApprover.Verify(publicApi);
        }
    }
}