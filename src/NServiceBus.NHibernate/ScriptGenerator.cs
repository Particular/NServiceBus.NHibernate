namespace NServiceBus.NHibernate
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using global::NHibernate.AdoNet.Util;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
    using NServiceBus.Outbox.NHibernate;
    using SagaPersisters.NHibernate.AutoPersistence;
    using Sagas;
    using Unicast.Subscriptions.NHibernate.Config;

    /// <summary>
    /// Allows offline schema generation.
    /// </summary>
    /// <typeparam name="T">Dialect.</typeparam>
    public partial class ScriptGenerator<T>
        where T : Dialect, new()
    {
        /// <summary>
        /// Generates the table creation script for the outbox.
        /// </summary>
        /// <param name="outboxRecordMappingType">Optional: custom outbox record mapping class.</param>
        public static string GenerateOutboxScript(Type outboxRecordMappingType = null)
        {
            return GenerateScript(outboxRecordMappingType ?? typeof(OutboxRecordMapping));
        }

        /// <summary>
        /// Generates the table creation script for the subscription store.
        /// </summary>
        public static string GenerateSubscriptionStoreScript()
        {
            return GenerateScript(typeof(SubscriptionMap));
        }

        /// <summary>
        /// Generates the table creation script for the saga data table
        /// </summary>
        /// <param name="tableNamingConvention">Optional custom table naming convention.</param>
        /// <typeparam name="TSaga">Saga type.</typeparam>
        public static string GenerateSagaScript<TSaga>(Func<Type, string> tableNamingConvention = null)
            where TSaga : Saga
        {
            var sagaBase = typeof(TSaga).BaseType;
            var sagaDataType = sagaBase.GetGenericArguments()[0];

            var metadata = new SagaMetadataCollection();
            metadata.Initialize(new[]
            {
                typeof(TSaga)
            });
            var typesToScan = new List<Type>
            {
                sagaDataType
            };
            var sagaDataBase = sagaDataType.BaseType;
            while (sagaDataBase != null)
            {
                typesToScan.Add(sagaDataBase);
                sagaDataBase = sagaDataBase.BaseType;
            }

            var config = new Configuration();
            config.DataBaseIntegration(db => { db.Dialect<T>(); });
            SagaModelMapper.AddMappings(config, metadata, typesToScan, tableNamingConvention);
            return GenerateScript(config);
        }

        static string GenerateScript(Type mappingType)
        {
            var config = new Configuration();
            config.DataBaseIntegration(db => { db.Dialect<T>(); });

            var mapper = new ModelMapper();
            mapper.AddMapping(mappingType);
            config.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            return GenerateScript(config);
        }

        static string GenerateScript(Configuration config)
        {
            var export = new SchemaExport(config);
            var formatter = FormatStyle.Ddl.Formatter;

            var script = new StringBuilder();
            export.Create(s => script.Append(formatter.Format(s)), false);
            return script.ToString();
        }
    }
}
