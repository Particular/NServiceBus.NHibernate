using System;
using NServiceBus;
using NServiceBus.Pipeline;
using NServiceBus.Transports;
using NServiceBus.Transports.Msmq;

namespace Test.NHibernate
{
    public class ChaosMonkeySender:ISendMessages
    {
        public MsmqMessageSender Inner { get; set; }
        public static bool BlowUpAfterDispatch { get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }

        public void Send(TransportMessage message, Address address)
        {
            Inner.Send(message,address);

            if (BlowUpAfterDispatch && PipelineExecutor.CurrentContext.Get<bool>("Outbox_StartDispatching"))
            {
                BlowUpAfterDispatch = false;
                Console.Out.WriteLine("Monkey: Message {0} dispatched, blowing up now like you asked me to!",message.Id);
                throw new Exception("BlowUpAfterDispatch");
            }
        }
    }
}