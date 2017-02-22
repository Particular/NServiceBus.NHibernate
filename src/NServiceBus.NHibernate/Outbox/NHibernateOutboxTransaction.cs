namespace NServiceBus.Outbox.NHibernate
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;
    using NServiceBus.Outbox;

    [SkipWeaving]
    class NHibernateOutboxTransaction : OutboxTransaction
    {
        public NHibernateOutboxTransaction(ISession session, ITransaction transaction)
        {
            Session = session;
            Transaction = transaction;
        }

        public ISession Session { get; }
        public ITransaction Transaction { get; }

        public void Dispose()
        {
            Session.Dispose();
        }

        public void OnSaveChanges(Func<Task> callback)
        {
            callbacks.Add(callback);
        }

        public async Task Commit()
        {
            await callbacks.InvokeAll().ConfigureAwait(false);

            Transaction.Commit();
            Transaction.Dispose();
        }

        CallbackList callbacks = new CallbackList();
    }
}