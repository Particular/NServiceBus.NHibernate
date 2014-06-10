namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using Persistence;

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
        public static void SagaTableNamingConvention(this PersistenceConfiguration persistenceConfiguration, Func<Type, string> tableNamingConvention)
        {
            persistenceConfiguration.Config.Settings.Set("NHibernate.Sagas.TableNamingConvention", tableNamingConvention);
        }
    }
}