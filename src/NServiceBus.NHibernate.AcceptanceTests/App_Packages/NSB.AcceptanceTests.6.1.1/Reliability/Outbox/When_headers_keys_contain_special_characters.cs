﻿namespace NServiceBus.AcceptanceTests.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_headers_contain_special_characters : NServiceBusAcceptanceTest
    {
        static Guid messageId = Guid.NewGuid();
        static bool SavedOutBoxRecord = false;
        static Dictionary<string, string> sentHeaders = new Dictionary<string, string>
                          {
                            { "a-B1", "a-B" },
                            { "a-B2", "a-ɤϡ֎ᾣ♥-b" },
                            { "a-ɤϡ֎ᾣ♥-B3", "a-B" },
                            { "a-B4", "a-\U0001F60D-b" },
                            { "a-\U0001F605-B5", "a-B" },
                            { "a-B6", "a-😍-b" },
                            { "a-😅-B7", "a-B" },
                            {"a-b8", "奥曼克"},
                            {"a-B9", "٩(-̮̮̃-̃)۶ ٩(●̮̮̃•̃)۶ ٩(͡๏̯͡๏)۶ ٩(-̮̮̃•̃)" },
                            {"a-b10", "தமிழ்" }
                        };

        [Test]
        public async Task Outbox_should_work()
        {
            var context =
                await Scenario.Define<Context>()
                .WithEndpoint<OutboxEndpoint>(b => b.When(session => session.SendLocal(new PlaceOrder())))
                .Done(c => c.MessageReceived)
                .Run(TimeSpan.FromSeconds(20));

            Assert.IsNotEmpty(context.UnicodeHeaders);
            CollectionAssert.IsSupersetOf(context.UnicodeHeaders, sentHeaders);
        }

        class Context : ScenarioContext
        {
            public Dictionary<string,string> UnicodeHeaders { get; set; }
            public bool MessageReceived { get; set; }

            public bool Counter { get; set; }
        }

        public class OutboxEndpoint : EndpointConfigurationBuilder
        {
            public OutboxEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.GetSettings().Set("DisableOutboxTransportCheck", true);
                    b.Pipeline.Register("BlowUpBeforeDispatchBehavior", new BlowUpBeforeDispatchBehavior(), "For testing");
                    b.Recoverability().Immediate(a => a.NumberOfRetries(1));
                    b.EnableOutbox();
                });
            }

            class BlowUpBeforeDispatchBehavior : IBehavior<IBatchDispatchContext, IBatchDispatchContext>
            {
                public async Task Invoke(IBatchDispatchContext context, Func<IBatchDispatchContext, Task> next)
                {
                    if (!SavedOutBoxRecord)
                    {
                        SavedOutBoxRecord = true;
                        throw new Exception();
                    }
                    

                    await next(context).ConfigureAwait(false);
                }
            }

            class PlaceOrderHandler : IHandleMessages<PlaceOrder>
            {
                public Task Handle(PlaceOrder message, IMessageHandlerContext context)
                {
                    var sendOrderAcknowledgement = new SendOrderAcknowledgement();
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    foreach (var header in sentHeaders)
                    {
                        sendOptions.SetHeader(header.Key, header.Value);
                    }
                    sendOptions.SetMessageId(messageId.ToString());
                    return context.Send(sendOrderAcknowledgement, sendOptions);
                }
            }

            class SendOrderAcknowledgementHandler : IHandleMessages<SendOrderAcknowledgement>
            {
                public Context Context { get; set; }

                public Task Handle(SendOrderAcknowledgement message, IMessageHandlerContext context)
                {
                    Context.MessageReceived = true;
                    Context.UnicodeHeaders = (Dictionary<string, string>) context.MessageHeaders;
                    return Task.FromResult(0);
                }
            }
        }

        public class PlaceOrder : ICommand
        {
        }

        public class SendOrderAcknowledgement : IMessage
        {
        }
    }

    
}