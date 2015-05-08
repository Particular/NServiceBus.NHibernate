
namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NUnit.Framework;
    using Saga;

    public class When_doing_request_response_between_sagas : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_autocorrelate_the_response_back_to_the_requesting_saga()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new InitiateRequestingSaga())))
                    .Done(c =>
                    {
                        return c.DidRequestingSagaGetTheResponseFromInitiatingHandler
                               && c.DidRequestingSagaGetTheResponseFromNonInitiatingHandler
                               && c.DidRequestingSagaGetTheResponseFromTimeout;
                    })
                    .Run(new RunSettings { UseSeparateAppDomains = true });

            Assert.True(context.DidRequestingSagaGetTheResponseFromInitiatingHandler);
            Assert.True(context.DidRequestingSagaGetTheResponseFromNonInitiatingHandler);
            Assert.True(context.DidRequestingSagaGetTheResponseFromTimeout);
        }

        public class Context : ScenarioContext
        {
            public bool DidRequestingSagaGetTheResponseFromInitiatingHandler { get; set; }
            public bool DidRequestingSagaGetTheResponseFromNonInitiatingHandler { get; set; }
            public bool DidRequestingSagaGetTheResponseFromTimeout { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {

            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class RequestingSaga : Saga<RequestingSaga.RequestingSagaData>,
                IAmStartedByMessages<InitiateRequestingSaga>,
                IHandleMessages<ResponseFromOtherSagaFromInitiatingHandler>,
                IHandleMessages<ResponseFromOtherSagaFromNonInitiatingHandler>,
                IHandleMessages<ResponseFromOtherSagaFromTimeout>
            {
                public Context Context { get; set; }

                public void Handle(InitiateRequestingSaga message)
                {
                    Data.CorrIdForResponse = Guid.NewGuid(); //wont be needed in the future

                    Bus.SendLocal(new RequestToRespondingSaga
                    {
                        SomeIdThatTheResponseSagaCanCorrelateBackToUs = Data.CorrIdForResponse //wont be needed in the future
                    });
                }

                public void Handle(ResponseFromOtherSagaFromInitiatingHandler message)
                {
                    Context.DidRequestingSagaGetTheResponseFromInitiatingHandler = true;
                }

                public void Handle(ResponseFromOtherSagaFromNonInitiatingHandler message)
                {
                    Context.DidRequestingSagaGetTheResponseFromNonInitiatingHandler = true;
                }

                public void Handle(ResponseFromOtherSagaFromTimeout message)
                {
                    Context.DidRequestingSagaGetTheResponseFromTimeout = true;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestingSagaData> mapper)
                {
                    //if this line is un-commented the timeout and secondary handler tests will start to fail
                    // for more info and discussion see TBD
                    mapper.ConfigureMapping<ResponseFromOtherSagaFromInitiatingHandler>(m => m.SomeCorrelationId).ToSaga(s => s.CorrIdForResponse);
                    mapper.ConfigureMapping<ResponseFromOtherSagaFromNonInitiatingHandler>(m => m.SomeCorrelationId).ToSaga(s => s.CorrIdForResponse);
                    mapper.ConfigureMapping<ResponseFromOtherSagaFromTimeout>(m => m.SomeCorrelationId).ToSaga(s => s.CorrIdForResponse);
                }
                public class RequestingSagaData : ContainSagaData
                {
                    [Unique]
                    public virtual Guid CorrIdForResponse { get; set; } //wont be needed in the future
                }

            }

            public class RespondingSaga : Saga<RespondingSaga.RespondingSagaData>,
                IAmStartedByMessages<RequestToRespondingSaga>,
                IHandleTimeouts<RespondingSaga.DelayReply>,
                IHandleMessages<SendReplyFromNonInitiatingHandler>
            {
                public Context Context { get; set; }

                public void Handle(RequestToRespondingSaga message)
                {
                    Data.CorrIdForRequest = message.SomeIdThatTheResponseSagaCanCorrelateBackToUs; //wont be needed in the future
                    Bus.SendLocal(new SendReplyFromNonInitiatingHandler { SagaIdSoWeCanCorrelate = Data.Id });

                    RequestTimeout<DelayReply>(TimeSpan.FromSeconds(1));

                    // Both reply and reply to originator work here since the sender of the incoming message is the requesting saga
                    // also note we don't set the correlation ID since auto correlation happens to work for this special case 
                    // where we reply from the first handler
                    Bus.Reply(new ResponseFromOtherSagaFromInitiatingHandler());
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RespondingSagaData> mapper)
                {
                    //this line is just needed so we can test the non initiating handler case
                    mapper.ConfigureMapping<SendReplyFromNonInitiatingHandler>(m => m.SagaIdSoWeCanCorrelate).ToSaga(s => s.Id);
                }

                public class RespondingSagaData : ContainSagaData
                {
                    [Unique]
                    public virtual Guid CorrIdForRequest { get; set; }
                }


                public class DelayReply { }

                public void Timeout(DelayReply state)
                {
                    //reply to originator must be used here since the sender of the incoming message the timeoutmanager and not the requesting saga
                    ReplyToOriginator(new ResponseFromOtherSagaFromTimeout //change this line to Bus.Reply(new ResponseFromOtherSaga  and see it fail
                    {
                        SomeCorrelationId = Data.CorrIdForRequest //wont be needed in the future
                    });
                }

                public void Handle(SendReplyFromNonInitiatingHandler message)
                {
                    //reply to originator must be used here since the sender of the incoming message the timeoutmanager and not the requesting saga
                    ReplyToOriginator(new ResponseFromOtherSagaFromNonInitiatingHandler //change this line to Bus.Reply(new ResponseFromOtherSaga  and see it fail
                    {
                        SomeCorrelationId = Data.CorrIdForRequest //wont be needed in the future
                    });
                }
            }
        }

        public class InitiateRequestingSaga : ICommand { }

        public class RequestToRespondingSaga : ICommand
        {
            public Guid SomeIdThatTheResponseSagaCanCorrelateBackToUs { get; set; }
        }

        public class ResponseFromOtherSagaFromInitiatingHandler : IMessage
        {
            public Guid SomeCorrelationId { get; set; }
        }

        public class ResponseFromOtherSagaFromNonInitiatingHandler : IMessage
        {
            public Guid SomeCorrelationId { get; set; }
        }

        public class ResponseFromOtherSagaFromTimeout : IMessage
        {
            public Guid SomeCorrelationId { get; set; }
        }

        public class SendReplyFromNonInitiatingHandler : ICommand
        {
            public Guid SagaIdSoWeCanCorrelate { get; set; }
        }
    }
}
