namespace NServiceBus.TransactionalSession
{
    class NHibernateTransactionalSessionOptionsCustomization : IOpenSessionOptionsCustomization
    {
        readonly string endpointName;

        public NHibernateTransactionalSessionOptionsCustomization(string endpointName) => this.endpointName = endpointName;

        public void Apply(OpenSessionOptions options)
        {
            if (options is NHibernateOpenSessionOptions nhOptions)
            {
                nhOptions.SetEndpointQualifiedMessageIdValue(endpointName);
            }
        }
    }
}