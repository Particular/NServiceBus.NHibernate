# NServiceBus.NHibernate

Persistence support for [NServiceBus](https://github.com/Particular/NServiceBus) using the [NHibernate](http://nhibernate.info/) ORM.

Learn more about NServiceBus.NHibernate through our [documentation](https://docs.particular.net/nservicebus/nhibernate/).

## Testing locally

The solution consists of a number of test projects with different requirements.

### Oracle-based tests

Requires an environment variable named `OracleConnectionString`. If this variable does not exist, the tests will fail. If the variable is not set and the `CI` environment variable is set to `true`, the tests will be ignored instead.

Note that some Oracle-based tests will fail on Windows if the Distributed Transaction Coordinator is turned off. These tests do not run on Linux.

**NServiceBus.AcceptanceTests**
**NServiceBus.AcceptanceTests-SqlTransport**
**NServiceBus.PersistenceTests**