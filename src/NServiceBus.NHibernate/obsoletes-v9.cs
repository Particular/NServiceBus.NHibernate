#pragma warning disable 1591

#pragma warning disable IDE0065 // Misplaced using directive
using System;
using NHibernate.Dialect;
#pragma warning restore IDE0065 // Misplaced using directive

namespace NServiceBus.Features
{
    [ObsoleteEx(
        Message = "The timeout manager has been removed. Timeout storage configuration can be removed.",
        TreatAsErrorFromVersion = "9",
        RemoveInVersion = "10")]
    public class NHibernateTimeoutStorage : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Persistence.NHibernate
{
    using global::NHibernate.Cfg;

    [ObsoleteEx(
        Message = "The timeout manager has been removed. Timeout storage configuration can be removed.",
        TreatAsErrorFromVersion = "9",
        RemoveInVersion = "10")]
    public static class TimeoutConfig
    {
        public static PersistenceExtensions<NHibernatePersistence> DisableTimeoutStorageSchemaUpdate(
            this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration)
        {
            throw new NotImplementedException();
        }

        public static PersistenceExtensions<NHibernatePersistence> UseTimeoutStorageConfiguration(
            this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration, Configuration configuration)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.NHibernate
{
    partial class ScriptGenerator<T>
        where T : Dialect, new()
    {
        [ObsoleteEx(
            Message = "The timeout manager has been removed. Timeout storage script can be removed.",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public static string GenerateTimeoutStoreScript()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "NHibernate gateway persistence is deprecated. Use the new NServiceBus.Gateway.Sql dedicated package.",
            RemoveInVersion = "10",
            TreatAsErrorFromVersion = "9")]
        public static string GenerateGatewayDeduplicationStoreScript()
        {
            throw new NotImplementedException();
        }
    }
}