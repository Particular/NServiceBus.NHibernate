namespace NServiceBus.Features
{
    /// <summary>
    /// NHibernate Gateway Deduplication.
    /// </summary>
    [ObsoleteEx(
            Message = "NHibernate gateway persistence is deprecated. Use the new NServiceBus.Gateway.Sql dedicated package.",
            RemoveInVersion = "10.0.0",
            TreatAsErrorFromVersion = "9.0.0")]
    public class NHibernateGatewayDeduplication : Feature
    {
        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {

        }
    }
}