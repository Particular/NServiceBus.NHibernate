namespace NServiceBus.Persistence.NHibernate
{
    using global::NHibernate.Cfg;

    class NHibernateConfiguration
    {
        readonly Configuration configuration;
        readonly string connectionString;

        public NHibernateConfiguration(Configuration configuration, string connectionString)
        {
            this.configuration = configuration;
            this.connectionString = connectionString;
        }

        public Configuration Configuration
        {
            get { return configuration; }
        }

        public string ConnectionString
        {
            get { return connectionString; }
        }
    }
}