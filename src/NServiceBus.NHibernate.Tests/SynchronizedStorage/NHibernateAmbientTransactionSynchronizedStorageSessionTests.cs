﻿namespace NServiceBus.NHibernate.Tests.SynchronizedStorage
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using NHibernate.SynchronizedStorage;
    using NUnit.Framework;
    using Persistence.NHibernate;
    using Transport;

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

                var callbackInvoked = 0;

                //The open method creates the NHibernateLazyNativeTransactionSynchronizedStorageSession
                var syncSession = new NHibernateSynchronizedStorageSession(new SessionFactoryHolder(SessionFactory));
                var success = await syncSession.TryOpen(transportTransaction, new ContextBag());
                Assert.That(success, Is.True);
                var storageSession = syncSession.InternalSession;

                var __ = storageSession.Session; //Make sure session is initialized
                storageSession.OnSaveChanges((s, _) =>
                {
                    callbackInvoked++;
                    return Task.CompletedTask;
                });
                storageSession.OnSaveChanges((s, _) =>
                {
                    callbackInvoked++;
                    return Task.CompletedTask;
                });

                await syncSession.CompleteAsync();

                Assert.That(callbackInvoked, Is.EqualTo(2));

                scope.Complete();
            }
        }



        [Test]
        public async Task It_does_not_commit_if_callback_throws()
        {
            var entityId = Guid.NewGuid().ToString();

            using (var scope = new TransactionScope())
            {
                var transportTransaction = new TransportTransaction();
                transportTransaction.Set(Transaction.Current);

                //The open method creates the NHibernateLazyNativeTransactionSynchronizedStorageSession
                var syncSession = new NHibernateSynchronizedStorageSession(new SessionFactoryHolder(SessionFactory));
                var success = await syncSession.TryOpen(transportTransaction, new ContextBag());
                Assert.That(success, Is.True);
                var storageSession = syncSession.InternalSession;

                storageSession.Session.Save(new TestEntity { Id = entityId });
                storageSession.OnSaveChanges((s, _) =>
                {
                    throw new Exception("Simulated");
                });

                try
                {
                    await syncSession.CompleteAsync();
                    scope.Complete();
                }
                catch (Exception)
                {
                    //NOOP
                }

            }

            using (var session = SessionFactory.OpenSession())
            {
                var savedEntity = session.Get<TestEntity>(entityId);
                Assert.That(savedEntity, Is.Null);
            }
        }
    }
}