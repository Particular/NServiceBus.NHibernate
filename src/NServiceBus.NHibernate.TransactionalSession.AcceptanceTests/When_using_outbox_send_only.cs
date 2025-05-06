namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using NUnit.Framework;

public class When_using_outbox_send_only : NServiceBusAcceptanceTest
{
    [Test()]
    public async Task Should_send_messages_on_transactional_session_commit()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<SendOnlyEndpoint>(s => s.When(async (_, ctx) =>
            {
                using var scope = ctx.ServiceProvider.CreateScope();
                using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();
                await transactionalSession.Open(new NHibernateOpenSessionOptions());

                var options = new SendOptions();

                options.SetDestination(Conventions.EndpointNamingConvention.Invoke(typeof(AnotherEndpoint)));

                await transactionalSession.Send(new SampleMessage(), options);

                await transactionalSession.Commit(CancellationToken.None);
            }))
            .WithEndpoint<AnotherEndpoint>()
            .WithEndpoint<ProcessorEndpoint>()
            .Done(c => c.MessageReceived)
            .Run();

        Assert.That(context.MessageReceived, Is.True);
    }

    [Test]
    public void Should_throw_when_processor_address_not_specified()
    {
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await Scenario.Define<Context>()
                .WithEndpoint<SendOnlyEndpointWithoutProcessor>()
                .Done(c => c.MessageReceived)
                .Run();
        });

        Assert.That(exception?.Message, Is.EqualTo("A configured ProcessorAddress is required when using the transactional session and the outbox with send-only endpoints"));
    }

    class Context : TransactionalSessionTestContext
    {
        public bool MessageReceived { get; set; }
    }

    class SendOnlyEndpoint : EndpointConfigurationBuilder
    {
        public SendOnlyEndpoint() => EndpointSetup<TransactionSessionWithOutboxEndpoint>((c, runDescriptor) =>
        {
            var options = new TransactionalSessionOptions { ProcessorAddress = Conventions.EndpointNamingConvention.Invoke(typeof(ProcessorEndpoint)) };

            var persistence = c.UsePersistence<NHibernatePersistence>();

            persistence.EnableTransactionalSession(options);

            c.EnableOutbox();
            c.SendOnly();
        });
    }

    class SendOnlyEndpointWithoutProcessor : EndpointConfigurationBuilder
    {
        public SendOnlyEndpointWithoutProcessor() => EndpointSetup<TransactionSessionWithOutboxEndpoint>(c =>
        {
            var persistence = c.UsePersistence<NHibernatePersistence>();

            // Deliberately not passing a ProcessorAddress via TransactionalSessionOptions
            persistence.EnableTransactionalSession();

            c.EnableOutbox();
            c.SendOnly();
        });
    }

    class AnotherEndpoint : EndpointConfigurationBuilder
    {
        public AnotherEndpoint() => EndpointSetup<DefaultServer>();

        class SampleHandler(Context testContext) : IHandleMessages<SampleMessage>
        {
            public Task Handle(SampleMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;

                return Task.CompletedTask;
            }
        }
    }

    class ProcessorEndpoint : EndpointConfigurationBuilder
    {
        public ProcessorEndpoint() => EndpointSetup<TransactionSessionWithOutboxEndpoint>((c, runDescriptor) =>
        {
            c.EnableOutbox();
            c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;

            var persistence = c.UsePersistence<NHibernatePersistence>();

            var options = new TransactionalSessionOptions();

            persistence.EnableTransactionalSession(options);
        }
        );
    }

    class SampleMessage : ICommand
    {
    }
}