﻿namespace NServiceBus
{
    using System;
    using System.Transactions;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Features;
    using NServiceBus.Outbox;

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
            outboxSettings.GetSettings().Set(NHibernateOutbox.OutboxConcurrencyModeSettingsKey, true);
        }

        /// <summary>
        /// Configures the outbox to use specific transaction level.
        /// Only levels Read Committed, Repeatable Read and Serializable are supported.
        /// </summary>
        public static void TransactionIsolationLevel(this OutboxSettings outboxSettings, System.Data.IsolationLevel isolationLevel)
        {
            if (isolationLevel is System.Data.IsolationLevel.Chaos
                or System.Data.IsolationLevel.ReadUncommitted
                or System.Data.IsolationLevel.Snapshot
                or System.Data.IsolationLevel.Unspecified)
            {
                throw new Exception($"Isolation level {isolationLevel} is not supported.");
            }
            outboxSettings.GetSettings().Set(NHibernateOutbox.OutboxTransactionIsolationLevelSettingsKey, isolationLevel);
        }

        /// <summary>
        /// Configures the outbox to use TransactionScope instead of native NHibernate transactions. This allows extending the
        /// scope of the outbox transaction (and synchronized storage session it protects) to other databases provided that
        /// Distributed Transaction Coordinator (DTC) infrastructure is configured.
        ///
        /// Uses the default isolation level (Serializable).
        /// </summary>
        public static void UseTransactionScope(this OutboxSettings outboxSettings)
        {
            UseTransactionScope(outboxSettings, IsolationLevel.Serializable);
        }

        /// <summary>
        /// Configures the outbox to use TransactionScope instead of native NHibernate transactions. This allows extending the
        /// scope of the outbox transaction (and synchronized storage session it protects) to other databases provided that
        /// Distributed Transaction Coordinator (DTC) infrastructure is configured.
        /// </summary>
        /// <param name="outboxSettings">Outbox settings.</param>
        /// <param name="isolationLevel">Isolation level to use. Only levels Read Committed, Repeatable Read and Serializable are supported.</param>
        public static void UseTransactionScope(this OutboxSettings outboxSettings, IsolationLevel isolationLevel)
        {
            if (isolationLevel is IsolationLevel.Chaos
                or IsolationLevel.ReadUncommitted
                or IsolationLevel.Snapshot
                or IsolationLevel.Unspecified)
            {
                throw new Exception($"Isolation level {isolationLevel} is not supported.");
            }
            outboxSettings.GetSettings().Set(NHibernateOutbox.OutboxTransactionModeSettingsKey, true);
            outboxSettings.GetSettings().Set(NHibernateOutbox.OutboxTransactionScopeModeIsolationLevelSettingsKey, isolationLevel);
        }
    }
}