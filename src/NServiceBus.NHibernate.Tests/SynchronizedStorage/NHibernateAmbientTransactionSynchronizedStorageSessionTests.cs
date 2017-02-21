namespace NServiceBus.NHibernate.Tests.SynchronizedStorage
{
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.NHibernate;
    using NServiceBus.Transport;
    using NUnit.Framework;

    [TestFixture]
    class NHibernateAmbientTransactionSynchronizedStorageSessionTests : InMemoryDBFixture
    {
        [Test]
        public async Task It_invokes_callbacks_when_session_is_completed()
        {
            using (var scope = new TransactionScope())
            {
                var transportTransaction = new TransportTransaction();
                transportTransaction.Set(Transaction.Current);

                var callbackInvoked = false;
                var adapter = new NHibernateSynchronizedStorageAdapter(SessionFactory);

                using (var storageSession = await adapter.TryAdapt(transportTransaction, new ContextBag()))
                {
                    storageSession.Session(); //Make sure session is initialized
                    storageSession.RegisterCommitHook(() =>
                    {
                        callbackInvoked = true;
                        return Task.FromResult(0);
                    });

                    await storageSession.CompleteAsync();

                    Assert.IsTrue(callbackInvoked);
                }

                scope.Complete();
            }
        }
    }
}