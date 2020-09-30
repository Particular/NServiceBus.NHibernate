namespace NServiceBus.Outbox.NHibernate
{
    using System;

    /// <summary>
    /// Represents the outbox record in the database.
    /// </summary>
    public interface IOutboxRecord
    {
        /// <summary>
        /// Id of the incoming message.
        /// </summary>
        string MessageId { get; set; }

        /// <summary>
        /// Gets or sets if the messages has already been dispatched to destinations.
        /// </summary>
        bool Dispatched { get; set; }

        /// <summary>
        /// Gets or sets when the messages has been dispatched.
        /// </summary>
        DateTime? DispatchedAt { get; set; }

        /// <summary>
        /// Gets or sets the serialized transport operations.
        /// </summary>
        string TransportOperations { get; set; }
    }
}