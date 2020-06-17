﻿namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using Features;
    using Outbox;

    /// <summary>
    /// Contains extensions methods which allow to configure NHibernate persistence specific outbox configuration
    /// </summary>
    public static class OutboxSettingsExtensions
    {
        /// <summary>
        /// Enables outbox pessimistic mode in which Outbox record is created prior to invoking the message handler. As a result,
        /// the likelihood of invoking the message handler multiple times in case of duplicate messages is much lower.
        ///
        /// Note that the outbox always ensures that the transactional side effects of message processing are applied once. The pessimistic
        /// mode only affects non-transactional side effects. In the pessimistic mode the latter are less likely to be applied
        /// multiple times but that can still happen e.g. when a message processing attempt is interrupted.
        /// </summary>
        public static void UsePessimisticConcurrencyControl(this OutboxSettings outboxSettings)
        {
            outboxSettings.GetSettings().Set(NHibernateStorageSession.OutboxConcurrencyModeSettingsKey, true);
        }

        /// <summary>
        /// Configures the outbox to use TransactionScope instead of native NHibernate transactions. This allows extending the
        /// scope of the outbox transaction (and synchronized storage session it protects) to other databases provided that
        /// Distributed Transaction Coordinator (DTC) infrastructure is configured.
        /// </summary>
        public static void UseTransactionScope(this OutboxSettings outboxSettings)
        {
            outboxSettings.GetSettings().Set(NHibernateStorageSession.OutboxTransactionModeSettingsKey, true);
        }
    }
}