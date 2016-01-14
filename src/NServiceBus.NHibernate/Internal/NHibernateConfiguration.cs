namespace NServiceBus.Persistence.NHibernate
{
    using global::NHibernate.Cfg;

    class NHibernateConfiguration
    {
        public NHibernateConfiguration(Configuration configuration, string connectionString)
        {
            Configuration = configuration;
            ConnectionString = connectionString;
        }

        public Configuration Configuration { get; }

        public string ConnectionString { get; }
    }
}