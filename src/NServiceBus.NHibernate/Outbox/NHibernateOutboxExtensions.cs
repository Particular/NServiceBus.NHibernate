namespace NServiceBus.NHibernate.Outbox
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Outbox;

    /// <summary>
    /// Contains extensions methods which allow to configure RavenDB outbox specific configuration
    /// </summary>
    public static class NHibernateOutboxExtensions
    {
        internal const string TimeToKeepDeduplicationDataSettingsKey = "Outbox.TimeToKeepDeduplicationData";
        internal const string FrequencyToRunDeduplicationDataCleanupSettingsKey = "Outbox.FrequencyToRunDeduplicationDataCleanup";

        internal const string TimeToKeepDeduplicationDataAppSetting = "NServiceBus/Outbox/NHibernate/TimeToKeepDeduplicationData";
        internal const string FrequencyToRunDeduplicationDataCleanupAppSetting = "NServiceBus/Outbox/NHibernate/FrequencyToRunDeduplicationDataCleanup";

        /// <summary>
        /// Sets the time to keep the deduplication data to the specified time span.
        /// </summary>
        /// <param name="configuration">The configuration being extended</param>
        /// <param name="timeToKeepDeduplicationData">The time to keep the deduplication data. 
        /// The cleanup process removes entries older than the specified time to keep deduplication data, therefore the time span cannot be negative</param>
        /// <returns>The configuration</returns>
        public static OutboxSettings TimeToKeepDeduplicationData(this OutboxSettings configuration, TimeSpan timeToKeepDeduplicationData)
        {
            if (timeToKeepDeduplicationData <= TimeSpan.Zero)
            {
                throw new ArgumentException("Specify a non-negative TimeSpan. The cleanup process removes entries older than the specified time to keep deduplication data, therefore the time span cannot be negative.", "timeToKeepDeduplicationData");
            }

            configuration.GetSettings().Set(TimeToKeepDeduplicationDataSettingsKey, timeToKeepDeduplicationData);
            return configuration;
        }

        /// <summary>
        /// Sets the frequency to run the deduplication data cleanup task.
        /// </summary>
        /// <param name="configuration">The configuration being extended</param>
        /// <param name="frequencyToRunDeduplicationDataCleanup">The frequency to run the deduplication data cleanup task. By specifying a negative time span (-1) the cleanup task will never run.</param>
        /// <returns>The configuration</returns>
        public static OutboxSettings FrequencyToRunDeduplicationDataCleanup(this OutboxSettings configuration, TimeSpan frequencyToRunDeduplicationDataCleanup)
        {
            configuration.GetSettings().Set("Outbox.FrequencyToRunDeduplicationDataCleanup", frequencyToRunDeduplicationDataCleanup);
            return configuration;
        }
    }
}
