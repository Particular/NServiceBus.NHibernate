namespace NServiceBus.NHibernate.Tests
{
    static class Consts
    {
        const string @default = @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True;";

        public static string SqlConnectionString
        {
            get
            {
                var env = System.Environment.GetEnvironmentVariable("SQLServerConnectionString");
                if (!string.IsNullOrEmpty(env))
                {
                    return env;
                }

                return @default;
            }
        }
    }
}