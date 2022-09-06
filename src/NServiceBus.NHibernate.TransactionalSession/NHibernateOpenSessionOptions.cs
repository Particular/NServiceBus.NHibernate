namespace NServiceBus.TransactionalSession
{
    /// <summary>
    /// The options allowing to control the behavior of the transactional session.
    /// </summary>
    public class NHibernateOpenSessionOptions : OpenSessionOptions
    {
        internal void SetEndpointQualifiedMessageIdValue(string endpointName)
        {
            var endpointQualifiedMessageId = $"{endpointName}/{SessionId}";
            var endpointQualifiedMessageIdKeyName = "NServiceBus.Persistence.NHibernate.EndpointQualifiedMessageId";

            Extensions.Set(endpointQualifiedMessageIdKeyName, endpointQualifiedMessageId);
            Metadata.Add(endpointQualifiedMessageIdKeyName, endpointQualifiedMessageId);
        }
    }
}