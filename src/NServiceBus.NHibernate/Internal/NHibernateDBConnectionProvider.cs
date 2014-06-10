namespace NServiceBus.Features
{
    using Persistence.NHibernate;

    /// <summary>
    /// Provides access to connections available on the pipeline
    /// </summary>
    public class NHibernateDBConnectionProvider:Feature
    {
        /// <summary>
        /// Registers the connection provider in  DI
        /// </summary>
        /// <param name="context"></param>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<DbConnectionProvider>(DependencyLifecycle.InstancePerCall);
        }
    }
}