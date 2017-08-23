namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using Configuration.AdvancedExtensibility;

    /// <summary>
    /// Saga configuration extensions.
    /// </summary>
    public static class SagaConfig
    {
        /// <summary>
        /// Sets the convention to use for naming tables.
        /// </summary>
        /// <param name="persistenceConfiguration"></param>
        /// <param name="tableNamingConvention">Convention to use for naming tables.</param>
        public static PersistenceExtensions<NHibernatePersistence> SagaTableNamingConvention(this PersistenceExtensions<NHibernatePersistence> persistenceConfiguration, Func<Type, string> tableNamingConvention)
        {
            persistenceConfiguration.GetSettings().Set("NHibernate.Sagas.TableNamingConvention", tableNamingConvention);
            return persistenceConfiguration;
        }
    }
}