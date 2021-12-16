# NServiceBus.NHibernate

Persistence support for [NServiceBus](https://github.com/Particular/NServiceBus) using the [NHibernate](http://nhibernate.info/) ORM.

Learn more about NServiceBus.NHibernate through our [documentation](https://docs.particular.net/nservicebus/nhibernate/).

## Running the tests

The solution consists of a number of test projects with different requirements. In general, you can run all the tests safely if you have both: a SQL Server instance named `SQLExpress` with a database named `nservicebus`, and an Oracle database with an environment variable named `OracleConnectionString` pointing to it. If you don't have an Oracle instance available, you can skip the `NServiceBus.NHibernate.AcceptanceTests-Oracle` project. Further details are below.

### NServiceBus.NHibernate.Tests

Unit tests for the NHibernate persister. These tests have no requirements.

### NServiceBus.NHibernate.PersistenceTests

These tests are inherited from [NServiceBus Core](https://github.com/Particular/NServiceBus/tree/master/src/NServiceBus.PersistenceTests). The project in this repository consists only of configuration for those tests, mainly in `PersistenceTestsConfiguration.cs`.

When running the tests locally, a SQL Server database is required and by default, the tests assume there is a SQL Express instance named `SQLExpress` with a database named `nservicebus`. This assumption can be overridden by providing an environment variable named `SqlServerConnectionString` pointing to the database instance of your choice.

Some persistence tests can also execute against an Oracle database. To do so, specify an environment variable named `OracleConnectionString` pointing the database of your choice. If this variable is not defined, the tests will not execute against Oracle.

In a CI environment (i.e. if the `CI` environment variable is set, as it is with GitHub Actions), a default database is not assumed in either SQL Server or Oracle; if neither the `SqlServerConnectionString` nor the `OracleConnectionString` environment variables are explicitly set, the persistence tests will all be ignored.

To summarize:

* When testing locally, make sure there is one or both of the following:
  * an Oracle database and an `OracleConnectionString` environment variable
  * EITHER a `SQLExpress` instance with a database named `nservicebus`, OR another SQL Server database and an environment variable named `SqlServerConnectionString` pointing to it
* When testing in GitHub Actions, make sure there is one or both of the following:
  * an Oracle database and an `OracleConnectionString` environment variable
  * a SQL Server database and an environment variable named `SqlServerConnectionString` pointing to it
  * if neither is supplied, the tests won't execute and will be ignored

### NServiceBus.NHibernate.AcceptanceTests-SqlTransportTests and NServiceBus.NHibernate.AcceptanceTests

These tests are inherited from [NServiceBus Core](https://github.com/Particular/NServiceBus/tree/master/src/NServiceBus.AcceptanceTests) and run a suite of tests using NHibernate as the persistence. The difference between the two suites of tests is the transport: in `AcceptanceTests-SqlTransportTests`, [SQL Server](https://github.com/Particular/NServiceBus.sqlserver) is used as the transport while in `AcceptanceTests`, the tests use a custom [`AcceptanceTestingTransport`](https://github.com/Particular/NServiceBus/blob/master/src/NServiceBus.AcceptanceTesting/AcceptanceTestingTransport/AcceptanceTestingTransport.cs).

When running the tests locally, a SQL Server database is required and by default, the tests assume there is a SQL Express instance named `SQLExpress` with a database named `nservicebus`. This assumption can be overridden by providing an environment variable named `SqlServerConnectionString` pointing to the database instance of your choice.

In a CI environment (i.e. if the `CI` environment variable is set, as it is with GitHub Actions), a default database is not assumed; if the `SqlServerConnectionString` environment variable is not explicitly set, the tests in this project will be ignored.

### NServiceBus.NHibernate.AcceptanceTests-Oracle

These tests are inherited from [NServiceBus Core](https://github.com/Particular/NServiceBus/tree/master/src/NServiceBus.AcceptanceTests) and run a suite of tests using NHibernate as the persistence and a custom [`AcceptanceTestingTransport`](https://github.com/Particular/NServiceBus/blob/master/src/NServiceBus.AcceptanceTesting/AcceptanceTestingTransport/AcceptanceTestingTransport.cs). This project executes the acceptance tests using Oracle as the database for the NHibernate persister and with a custom [`AcceptanceTestingTransport`](https://github.com/Particular/NServiceBus/blob/master/src/NServiceBus.AcceptanceTesting/AcceptanceTestingTransport/AcceptanceTestingTransport.cs).

When running the tests locally, an Oracle database is required along with an environment variable named `OracleConnectionString` pointing to it.

In a CI environment (i.e. if the `CI` environment variable is set, as it is with GitHub Actions), if the `OracleConnectionString` environment variable is not explicitly set, the tests in this project will be ignored.
