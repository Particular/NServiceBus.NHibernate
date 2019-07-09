namespace NServiceBus.NHibernate.Tests.TimeoutPersister
{
    using System.Data.SqlClient;
    using System.Threading.Tasks;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
    using NUnit.Framework;
    using TimeoutPersisters.NHibernate.Config;
    using TimeoutPersisters.NHibernate.Installer;

    class When_checking_db_indexes
    {
        SchemaExport schemaExport;

        string dbSchemaName;
        bool dbSchemaNeedsQuoting;

        [Test]
        public async Task Should_detect_existing_TimeoutEntity_index_in_default_configuration()
        {
            var configuration = await CreateTimeoutManagerObjects();

            var validationResult = new TimeoutsIndexValidator(configuration).Validate();

            Assert.IsTrue(validationResult.IsValid, "Validation should pass for default db structure.");
        }

        [Test]
        public async Task Should_detect_missing_TimeoutEntity_index_in_unquoted_schema()
        {
            dbSchemaName = "some_schema";

            var configuration = await CreateTimeoutManagerObjects();
            await DropIndex();

            var validationResult = new TimeoutsIndexValidator(configuration).Validate();

            Assert.IsFalse(validationResult.IsValid, "Validation should fail if the index is missing.");
        }

        [Test]
        public async Task Should_detect_existing_TimeoutEntity_index_in_quoted_schema()
        {
            dbSchemaName = "quoted-schema";
            dbSchemaNeedsQuoting = true;

            var configuration = await CreateTimeoutManagerObjects();

            var validationResult = new TimeoutsIndexValidator(configuration).Validate();

            Assert.IsTrue(validationResult.IsValid, "Validation should pass for existing index in quoted schema.");
        }

        [Test]
        public async Task Should_detect_missing_TimeoutEntity_index_in_quoted_schema()
        {
            dbSchemaName = "quoted-schema";
            dbSchemaNeedsQuoting = true;

            var configuration = await CreateTimeoutManagerObjects();
            await DropIndex();

            var validationResult = new TimeoutsIndexValidator(configuration).Validate();

            Assert.IsTrue(validationResult.IsValid, "Validation should fail if index is missing in quoted schema.");
        }

        async Task<Configuration> CreateTimeoutManagerObjects()
        {
            var configuration = new Configuration()
                .DataBaseIntegration(x =>
                {
                    x.Dialect<MsSql2012Dialect>();
                    x.ConnectionString = Consts.SqlConnectionString;
                });

            if (dbSchemaName != null)
            {
                await CreateDbSchema();

                configuration.SetProperty(Environment.DefaultSchema, dbSchemaNeedsQuoting ? $"[{dbSchemaName}]" : dbSchemaName);
            }

            var mapper = new ModelMapper();
            mapper.AddMapping<TimeoutEntityMap>();

            configuration.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            schemaExport = new SchemaExport(configuration);
            await schemaExport.CreateAsync(false, true);
            return configuration;
        }

        Task CreateDbSchema() => ExecuteCommand($"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'{dbSchemaName}') EXEC('CREATE SCHEMA [{dbSchemaName}] AUTHORIZATION [dbo]');");

        Task DropDbSchema() => ExecuteCommand($"DROP SCHEMA [{dbSchemaName}]");

        Task DropIndex() => ExecuteCommand($"DROP INDEX {TimeoutEntityMap.EndpointIndexName} ON [{dbSchemaName}].[TimeoutEntity]");

        static async Task ExecuteCommand(string commandText)
        {
            using (var connection = new SqlConnection(Consts.SqlConnectionString))
            {
                await connection.OpenAsync();

                await new SqlCommand(commandText, connection).ExecuteNonQueryAsync();
            }
        }

        [TearDown]
        public async Task TearDown()
        {
             await schemaExport.DropAsync(false, true);

             if (dbSchemaName != null)
             {
                 await DropDbSchema();
             }
        }
    }
}
