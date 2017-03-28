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
            Transaction.Commit();
            Transaction.Dispose();
        }

        Func<Task> onSaveChangesCallback;
    }
}