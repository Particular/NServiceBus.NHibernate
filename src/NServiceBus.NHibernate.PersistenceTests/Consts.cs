namespace NServiceBus.NHibernate.PersistenceTests
{
    using System;

    static class Consts
    {
        const string @default = @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True;";

        public static string ConnectionString
        {
            get
            {
                var env = Environment.GetEnvironmentVariable("NH_ACC_TEST_CONNSTR");
                if (!string.IsNullOrEmpty(env))
                {
                    return env;
                }

                env = Environment.GetEnvironmentVariable("SQLServerConnectionString");
                if (!string.IsNullOrEmpty(env))
                {
                    return env;
                }

                return @default;
            }
        }
    }
}