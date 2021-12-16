namespace NServiceBus.NHibernate.PersistenceTests
{
    using System;

    static class Consts
    {
        const string defaultConnStr = @"Data Source=localhost;Initial Catalog=nservicebus;Integrated Security=True;";

        public static string ConnectionString
        {
            get
            {
                var env = Environment.GetEnvironmentVariable("SQLServerConnectionString");
                var ci = Environment.GetEnvironmentVariable("CI");
                if (!string.IsNullOrEmpty(env))
                {
                    return env;
                }
                return ci == "true" ? null : defaultConnStr;
            }
        }
    }
}