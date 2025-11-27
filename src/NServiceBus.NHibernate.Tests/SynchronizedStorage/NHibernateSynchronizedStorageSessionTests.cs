namespace NServiceBus.NHibernate.Tests.SynchronizedStorage;

using NHibernate.SynchronizedStorage;
using NUnit.Framework;
using Persistence.NHibernate;

[TestFixture]
public class NHibernateSynchronizedStorageSessionTests
{
    [Test]
    public void Should_not_throw_when_disposing_without_opened_session()
    {
        var synchronizedSession = new NHibernateSynchronizedStorageSession(new SessionFactoryHolder(null));

        Assert.DoesNotThrow(() => synchronizedSession.Dispose());
    }
}