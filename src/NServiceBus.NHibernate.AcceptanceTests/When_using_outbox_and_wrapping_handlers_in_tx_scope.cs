namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using NServiceBus.Logging;
    using NUnit.Framework;

    public class When_using_outbox_and_wrapping_handlers_in_tx_scope : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_persist_saga_and_log_warning()
        {
            /*
             * DoNotFailOnErrorMessages is used here because the original problem discovered with the code was causing data loss due to incorrect transaction
             * handling in the outbox feature while FLR was enabled.
             */
            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<OutboxTransactionScopeSagaEndpoint>(b => b.When(async session =>
                {
                    var sagaId = Guid.NewGuid();
                    await session.SendLocal(new StartSagaMessage
                    {
                        UniqueId = sagaId
                    }).ConfigureAwait(false);
                    await session.SendLocal(new CheckSagaMessage
                    {
                        UniqueId = sagaId
                    }).ConfigureAwait(false);
                }).DoNotFailOnErrorMessages())
                .Done(c => c.Done)
                .Run(TimeSpan.FromSeconds(20));

            Assert.IsTrue(ctx.Done);
            Assert.IsTrue(ctx.SagaStarted);
            Assert.IsTrue(ctx.Logs.Any(x => x.Level == LogLevel.Warn && x.Message.StartsWith("The endpoint is configured to use Outbox but a TransactionScope has been detected.")));
        }

        class Context : ScenarioContext
        {
            public bool SagaStarted { get; set; }
            public bool Done { get; set; }
        }

        public class OutboxTransactionScopeSagaEndpoint : EndpointConfigurationBuilder
        {
            public OutboxTransactionScopeSagaEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.GetSettings().Set("DisableOutboxTransportCheck", true);
                    b.EnableOutbox();
                    b.UnitOfWork().WrapHandlersInATransactionScope();
                    b.LimitMessageProcessingConcurrencyTo(1); //To ensure saga is properly created before we check it.
                });
            }
            
            class OutboxTransactionScopeSaga : Saga<OutboxTransactionScopeSagaData>,
                IAmStartedByMessages<StartSagaMessage>,
                IAmStartedByMessages<CheckSagaMessage>
            {
                public Context Context { get; set; }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OutboxTransactionScopeSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.UniqueId).ToSaga(s => s.UniqueId);
                    mapper.ConfigureMapping<CheckSagaMessage>(m => m.UniqueId).ToSaga(s => s.UniqueId);
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Data.Started = true;
                    return Task.FromResult(0);
                }

                public Task Handle(CheckSagaMessage message, IMessageHandlerContext context)
                {
                    Context.SagaStarted = Data.Started;
                    Context.Done = true;
                    return Task.FromResult(0);
                }
            }

            public class OutboxTransactionScopeSagaData : ContainSagaData
            {
                public virtual bool Started { get; set; }
                public virtual Guid UniqueId { get; set; }
            }
        }

        public class StartSagaMessage : IMessage
        {
            public virtual Guid UniqueId { get; set; }
        }

        public class CheckSagaMessage : IMessage
        {
            public virtual Guid UniqueId { get; set; }
        }
    }

    
}