namespace NServiceBus
{
    using System;
    using NServiceBus.Outbox;

    interface INHibernateOutboxStorage : IOutboxStorage
    {
        void RemoveEntriesOlderThan(DateTime dateTime);
    }
}