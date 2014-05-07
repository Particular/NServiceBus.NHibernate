using System;
using System.Collections.Generic;
using NServiceBus.Outbox;

namespace Test.NHibernate
{
    public class ChaosMonkeyOutbox:IOutboxStorage
    {
        public OutboxPersister InnerPersister{ get; set; }
        public bool SkipGetOnce { get; set; }

        public bool TryGet(string messageId, out OutboxMessage message)
        {
            if (SkipGetOnce)
            {
                message = null;

                SkipGetOnce = false;

                Console.Out.WriteLine("Monkey: Message {0} was skipped, heheheh", messageId);
                return false;
            }
            var found=  InnerPersister.TryGet(messageId, out message);

            Console.Out.WriteLine("Monkey: Message {0} was {1} in outbox",messageId,found ? "found" : "not found");
            return found;
        }

        public IDisposable OpenSession()
        {
            return InnerPersister.OpenSession();
        }

        public void StoreAndCommit(string messageId, IEnumerable<TransportOperation> transportOperations)
        {
            try
            {
                InnerPersister.StoreAndCommit(messageId, transportOperations);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Monkey: Store and commit for message {0} blew up: {1}",messageId,ex.Message);
                
                throw;
            }
            
        }

        public void SetAsDispatched(string messageId)
        {
            InnerPersister.SetAsDispatched(messageId);
        }
    }
}