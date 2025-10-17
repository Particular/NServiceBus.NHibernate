#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NServiceBus
{
    using System;
    using Particular.Obsoletes;

    public partial class NHibernatePersistence
    {
        [ObsoleteMetadata(
            Message = "The NHibernatePersistence class is not supposed to be instantiated directly",
            RemoveInVersion = "12",
            TreatAsErrorFromVersion = "11")]
        [Obsolete("The NHibernatePersistence class is not supposed to be instantiated directly. Will be removed in version 12.0.0.", true)]
        public NHibernatePersistence() => throw new NotImplementedException();
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member