namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Saga;
    using NUnit.Framework;

    public class When_trying_to_integrate_with_sql_transport_with_transaction_scope_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public void It_throws_descriptive_exception()
        {
            try
            {
                Environment.SetEnvironmentVariable("Transport.UseSpecific", "SqlServerTransport");
                Scenario.Define(new Context())
                        .WithEndpoint<Endpoint>()
                        .AllowExceptions()
                        .Done(c => true)
                        .Run();

                Assert.Fail("Expected exception");
            }
            catch (AggregateException ex)
            {
                Assert.IsTrue(ex.InnerExceptions.Any(x => x.InnerException.Message.StartsWith("In order for NHibernate persistence to work with SQLServer transport")));
            }
        }

        public class Context : ScenarioContext
        {
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(bc => bc.Transactions().DisableDistributedTransactions());
            }

            //Force enabling sagas to ensure shared storage is used
            public class EmptySaga : Saga<EmptySagaData>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<EmptySagaData> mapper)
                {
                }
            }

            public class EmptySagaData : ContainSagaData
            {
            }
        }
    }
}
