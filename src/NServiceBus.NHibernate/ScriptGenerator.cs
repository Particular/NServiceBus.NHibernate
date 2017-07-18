﻿namespace NServiceBus.NHibernate
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using global::NHibernate.AdoNet.Util;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
    using NServiceBus.Deduplication.NHibernate.Config;
    using NServiceBus.Outbox.NHibernate;
    using NServiceBus.SagaPersisters.NHibernate.AutoPersistence;
    using NServiceBus.Sagas;
    using NServiceBus.TimeoutPersisters.NHibernate.Config;
    using NServiceBus.TimeoutPersisters.NHibernate.Installer;
    using Unicast.Subscriptions.NHibernate.Config;

    /// <summary>
    /// Allows offline schema generation.
    /// </summary>
    /// <typeparam name="T">Dialect.</typeparam>
    public class ScriptGenerator<T>
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
        /// <returns></returns>
        public static string GenerateSubscriptionStoreScript()
        {
            return GenerateScript(typeof(SubscriptionMap));
        }

        /// <summary>
        /// Generates the table creation script for the timeout store.
        /// </summary>
        /// <returns></returns>
        public static string GenerateTimeoutStoreScript()
        {
            return GenerateScript(typeof(TimeoutEntityMap));
        }

        /// <summary>
        /// Generates the table creation script for the Gateway deduplication store.
        /// </summary>
        /// <returns></returns>
        public static string GenerateGatewayDeduplicationStoreScript()
        {
            return GenerateScript(typeof(DeduplicationMessageMap));
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

            var sagaMapper = new SagaModelMapper(metadata, typesToScan, tableNamingConvention);

            var config = new Configuration();
            config.DataBaseIntegration(db => { db.Dialect<T>(); });
            config.AddMapping(sagaMapper.Compile());
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
            var dialect = new T();
            var export = new SchemaExport(config);
            var fixUpHelper = new SchemaFixUpHelper(config, dialect);
            var formatter = FormatStyle.Ddl.Formatter;
            var script = new StringBuilder();
            export.Create(s => script.Append(formatter.Format(fixUpHelper.FixUp(s))), false);
            return script.ToString();
        }
    }
}