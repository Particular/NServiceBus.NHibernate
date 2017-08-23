namespace NServiceBus
{
    using System;
    using Outbox;

    interface INHibernateOutboxStorage : IOutboxStorage
    {
        void RemoveEntriesOlderThan(DateTime dateTime);
    }
}