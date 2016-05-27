namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using Configuration.AdvanceExtensibility;

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
        public static PersistenceExtentions<NHibernatePersistence> SagaTableNamingConvention(this PersistenceExtentions<NHibernatePersistence> persistenceConfiguration, Func<Type, string> tableNamingConvention)
        {
            //TODO
            persistenceConfiguration.GetSettings().Set("NHibernate.Sagas.TableNamingConvention", tableNamingConvention);
            return persistenceConfiguration;
        }
    }
}