namespace NServiceBus.Outbox.NHibernate
{
    using System;
    using System.Threading.Tasks;
    using global::NHibernate;
    using Janitor;
    using Outbox;

    [SkipWeaving]
    class NHibernateOutboxTransaction : OutboxTransaction
    {
        public NHibernateOutboxTransaction(ISession session, ITransaction transaction)
        {
            Session = session;
            this.transaction = transaction;
        }

        public ISession Session { get; }

        public ITransaction Transaction => transaction;

        public void OnSaveChanges(Func<Task> callback)
        {
            if (onSaveChangesCallback != null)
            {
                throw new Exception("Save changes callback for this session has already been registered.");
            }
            onSaveChangesCallback = callback;
        }

        public async Task Commit()
        {
            if (onSaveChangesCallback != null)
            {
                await onSaveChangesCallback().ConfigureAwait(false);
            }
            transaction.Commit();
            transaction.Dispose();
            transaction = null;
        }

        public void Dispose()
        {
            //If save changes callback failed, we need to dispose the transaction here.
            if (transaction != null)
            {
                transaction.Dispose();
                transaction = null;
            }
            Session.Dispose();
        }

        Func<Task> onSaveChangesCallback;
        ITransaction transaction;
    }
}