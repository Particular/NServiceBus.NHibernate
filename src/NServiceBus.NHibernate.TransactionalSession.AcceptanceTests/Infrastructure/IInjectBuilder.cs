namespace NServiceBus.TransactionalSession.AcceptanceTests.Infrastructure
{
    using ObjectBuilder;

    public interface IInjectBuilder
    {
        IBuilder Builder { get; set; }
    }
}