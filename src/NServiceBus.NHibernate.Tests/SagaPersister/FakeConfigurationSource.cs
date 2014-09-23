namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using Config.ConfigurationSource;

    class FakeConfigurationSource : IConfigurationSource
    {
        public T GetConfiguration<T>() where T : class, new()
        {
            return default(T);
        }
    }
}