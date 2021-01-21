namespace Runner
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus;
    using NServiceBus.UnitOfWork;

    class StatisticsUoW : IManageUnitsOfWork, INeedInitialization
    {
        public Task Begin()
        {
            if (!Statistics.First.HasValue)
            {
                Statistics.First = DateTime.Now;
            }

            if (Transaction.Current != null)
            {
                Transaction.Current.TransactionCompleted += OnCompleted;
            }

            //            if (message.TwoPhaseCommit)
            //{
            //    Transaction.Current.EnlistDurable(Guid.NewGuid(), enlistment, EnlistmentOptions.None);
            //}static readonly TwoPhaseCommitEnlistment enlistment = new TwoPhaseCommitEnlistment();  

            return Task.FromResult(9);
        }

        void OnCompleted(object sender, TransactionEventArgs e)
        {
            if (e.Transaction.TransactionInformation.Status != TransactionStatus.Committed)
            {
                return;
            }

            RecordSuccess();
        }

        static void RecordSuccess()
        {
            Statistics.Last = DateTime.Now;
            Interlocked.Increment(ref Statistics.NumberOfMessages);
        }

        public Task End(Exception ex = null)
        {
            if (ex != null)
            {
                Interlocked.Increment(ref Statistics.NumberOfRetries);
                return Task.FromResult(0);
            }

            if (Transaction.Current == null)
            {
                RecordSuccess();
            }
            return Task.FromResult(0);
        }

        public void Customize(EndpointConfiguration builder)
        {
            builder.RegisterComponents(c => c.ConfigureComponent<StatisticsUoW>(DependencyLifecycle.InstancePerUnitOfWork));
        }
    }
}