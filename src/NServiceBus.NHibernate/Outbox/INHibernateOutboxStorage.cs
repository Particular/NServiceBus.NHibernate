﻿namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Outbox;

    interface INHibernateOutboxStorage : IOutboxStorage
    {
        Task RemoveEntriesOlderThan(DateTime dateTime, CancellationToken cancellationToken = default);
    }
}