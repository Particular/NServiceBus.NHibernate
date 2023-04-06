namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Data.SqlClient;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using Infrastructure;
    using NUnit.Framework;
    using ObjectBuilder;

    public class When_using_transactional_session_with_transactionscope : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_provide_ambient_transactionscope()
        {
            string rowId = Guid.NewGuid().ToString();

            await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.Builder.CreateChildBuilder();
                    using var transactionalSession = scope.Build<ITransactionalSession>();
                    await transactionalSession.Open();

                    await transactionalSession.SendLocal(new SampleMessage());

                    var storageSession = transactionalSession.SynchronizedStorageSession.Session();

                    string insertText =
                        $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SomeTable' and xtype='U')
                                        BEGIN
	                                        CREATE TABLE [dbo].[SomeTable]([Id] [nvarchar](50) NOT NULL)
                                        END;
                                        INSERT INTO [dbo].[SomeTable] VALUES ('{rowId}')";

                    await storageSession.CreateSQLQuery(insertText).ExecuteUpdateAsync(CancellationToken.None);

                    using (var __ = new TransactionScope(TransactionScopeOption.Suppress,
                               TransactionScopeAsyncFlowOption.Enabled))
                    {
                        using var connection = new SqlConnection(TransactionSessionDefaultServer.ConnectionString);

                        await connection.OpenAsync();

                        using var queryCommand =
                            new SqlCommand(
                                $"SELECT TOP 1 [Id] FROM [dbo].[SomeTable] WITH (READPAST) WHERE [Id]='{rowId}' ",
                                connection);
                        object result = await queryCommand.ExecuteScalarAsync();

                        Assert.AreEqual(null, result);
                    }

                    await transactionalSession.Commit().ConfigureAwait(false);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            using var connection = new SqlConnection(TransactionSessionDefaultServer.ConnectionString);

            await connection.OpenAsync();

            using var queryCommand =
                new SqlCommand($"SELECT TOP 1 [Id] FROM [dbo].[SomeTable] WHERE [Id]='{rowId}'", connection);
            object result = await queryCommand.ExecuteScalarAsync();

            Assert.AreEqual(rowId, result);
        }

        class Context : ScenarioContext, IInjectBuilder
        {
            public bool MessageReceived { get; set; }
            public bool CompleteMessageReceived { get; set; }
            public IBuilder Builder { get; set; }
        }

        class AnEndpoint : EndpointConfigurationBuilder
        {
            public AnEndpoint() =>
                EndpointSetup<TransactionSessionWithOutboxEndpoint>(c =>
                {
                    c.EnableOutbox().UseTransactionScope();
                });

            class SampleHandler : IHandleMessages<SampleMessage>
            {
                public SampleHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(SampleMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;

                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }

            class CompleteTestMessageHandler : IHandleMessages<CompleteTestMessage>
            {
                public CompleteTestMessageHandler(Context context) => testContext = context;

                public Task Handle(CompleteTestMessage message, IMessageHandlerContext context)
                {
                    testContext.CompleteMessageReceived = true;

                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }
        }

        class SampleMessage : ICommand
        {
        }

        class CompleteTestMessage : ICommand
        {
        }
    }
}