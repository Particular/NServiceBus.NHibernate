namespace NServiceBus.Outbox.NHibernate
{
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

        public Task Commit()
        {
            Transaction.Commit();
            Transaction.Dispose();

            return Task.FromResult(0);
        }
    }
}