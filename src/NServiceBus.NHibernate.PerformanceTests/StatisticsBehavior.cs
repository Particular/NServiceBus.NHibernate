namespace Runner
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Pipeline;

    class StatisticsBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            var success = true;
            try
            {
                if (!Statistics.First.HasValue)
                {
                    Statistics.First = DateTimeOffset.UtcNow;
                }

                if (Transaction.Current != null)
                {
                    Transaction.Current.TransactionCompleted += OnCompleted;
                }

                await next();
            }
            catch
            {
                success = false;
                throw;
            }
            finally
            {
                if (success == false)
                {
                    Interlocked.Increment(ref Statistics.NumberOfRetries);
                }
                else if (Transaction.Current == null)
                {
                    RecordSuccess();
                }
            }
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
            Statistics.Last = DateTimeOffset.UtcNow;
            Interlocked.Increment(ref Statistics.NumberOfMessages);
        }
    }
}