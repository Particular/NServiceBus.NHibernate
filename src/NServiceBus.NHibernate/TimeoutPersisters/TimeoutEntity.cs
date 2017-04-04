namespace NServiceBus.TimeoutPersisters.NHibernate
{
    using System;
    using Timeout.Core;

    /// <summary>
    /// NHibernate wrapper class for <see cref="TimeoutData"/>
    /// </summary>
    class TimeoutEntity
    {
        /// <summary>
        /// Id of this timeout.
        /// </summary>
        public virtual Guid Id { get; set; }

        /// <summary>
        /// The address of the client who requested the timeout.
        /// </summary>
        public virtual Address Destination { get; set; }

        /// <summary>
        /// The saga ID.
        /// </summary>
        public virtual Guid SagaId { get; set; }

        /// <summary>
        /// Additional state.
        /// </summary>
        public virtual byte[] State { get; set; }

        /// <summary>
        /// Timeout endpoint name.
        /// </summary>
        /// <remarks>
        /// It is important that this property is declared before the <see cref="Time"/> property
        /// for NHibernate to ensure TimeoutEntity_EndpointIdx has proper column order: (Endpoint, Time).
        /// </remarks>
        public virtual string Endpoint { get; set; }

        /// <summary>
        /// The time at which the saga ID expired.
        /// </summary>
        /// <remarks>
        /// It is important that this property is declared after the <see cref="Endpoint"/> property
        /// for NHibernate to ensure TimeoutEntity_EndpointIdx has proper column order: (Endpoint, Time).
        /// </remarks>
        public virtual DateTime Time { get; set; }

        /// <summary>
        /// Store the headers to preserve them across timeouts.
        /// </summary>
        public virtual string Headers { get; set; }
    }
}
