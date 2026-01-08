namespace NServiceBus.AcceptanceTests.Outbox
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Logging;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_using_transaction_scope_outbox_and_wrapping_handlers : NServiceBusAcceptanceTest
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
                }).DoNotFailOnErrorMessages())
                .Run();

            Assert.Multiple(() =>
            {
                Assert.That(ctx.SagaStarted, Is.True);
                Assert.That(ctx.Logs.Any(x => x.Level == LogLevel.Warn && x.Message.StartsWith("The endpoint is configured to use Outbox with TransactionScope but a different TransactionScope")), Is.True);
            });
        }

        class Context : ScenarioContext
        {
            public bool SagaStarted { get; set; }
        }

        public class OutboxTransactionScopeSagaEndpoint : EndpointConfigurationBuilder
        {
            public OutboxTransactionScopeSagaEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.Pipeline.Register(new BehaviorThatCreatesACustomScope(), "Creates a custom transaction scope");

                    b.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                    var outbox = b.EnableOutbox();
                    outbox.UseTransactionScope();
                });
            }

            class BehaviorThatCreatesACustomScope : Behavior<IIncomingLogicalMessageContext>
            {
                public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    using (new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        await next().ConfigureAwait(false);
                    }
                }
            }

            class OutboxTransactionScopeSaga(Context testContext) : Saga<OutboxTransactionScopeSagaData>,
                IAmStartedByMessages<StartSagaMessage>,
                IAmStartedByMessages<CheckSagaMessage>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OutboxTransactionScopeSagaData> mapper)
                {
                    mapper.MapSaga(s => s.UniqueId)
                        .ToMessage<StartSagaMessage>(m => m.UniqueId)
                        .ToMessage<CheckSagaMessage>(m => m.UniqueId);
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Data.Started = true;
                    return context.SendLocal(new StartSagaResponse
                    {
                        UniqueId = message.UniqueId
                    });
                }

                public Task Handle(CheckSagaMessage message, IMessageHandlerContext context)
                {
                    testContext.SagaStarted = Data.Started;
                    testContext.MarkAsCompleted();
                    return Task.FromResult(0);
                }
            }

            public class OutboxTransactionScopeSagaData : ContainSagaData
            {
                public virtual bool Started { get; set; }
                public virtual Guid UniqueId { get; set; }
            }
        }

        public class StartSagaResponseHandler : IHandleMessages<StartSagaResponse>
        {
            public Task Handle(StartSagaResponse message, IMessageHandlerContext context)
            {
                return context.SendLocal(new CheckSagaMessage
                {
                    UniqueId = message.UniqueId
                });
            }
        }

        public class StartSagaMessage : IMessage
        {
            public virtual Guid UniqueId { get; set; }
        }

        public class StartSagaResponse : IMessage
        {
            public virtual Guid UniqueId { get; set; }
        }

        public class CheckSagaMessage : IMessage
        {
            public virtual Guid UniqueId { get; set; }
        }
    }


}