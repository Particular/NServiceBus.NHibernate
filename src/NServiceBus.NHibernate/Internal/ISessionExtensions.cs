namespace NServiceBus.NHibernate.Internal
{
    using System.Data;
    using global::NHibernate;

    static class ISessionExtensions
    {
        internal static AmbientTransactionAwareWrapper BeginAmbientTransactionAware(this IStatelessSession session, IsolationLevel isolationLevel)
        {
            return new AmbientTransactionAwareWrapper(session, isolationLevel);
        }

        internal static AmbientTransactionAwareWrapper BeginAmbientTransactionAware(this ISession session, IsolationLevel isolationLevel)
        {
            return new AmbientTransactionAwareWrapper(session, isolationLevel);
        }
    }
}