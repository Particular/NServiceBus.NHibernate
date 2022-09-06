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

            Extensions.Set(EndpointQualifiedMessageIdKeyName, endpointQualifiedMessageId);
            Metadata.Add(EndpointQualifiedMessageIdKeyName, endpointQualifiedMessageId);
        }

        const string EndpointQualifiedMessageIdKeyName = "NServiceBus.Persistence.NHibernate.EndpointQualifiedMessageId";
    }
}