using NServiceBus;
using NServiceBus.Persistence;

public class ConfigureNHibernatePersistence
{
    public void Configure(BusConfiguration config)
    {
        config.UsePersistence<NHibernatePersistence>()
            .ConnectionString(ConnectionString);
    }

    public static string ConnectionString
    {
        get
        {
            string envVar = System.Environment.GetEnvironmentVariable("NH_ACC_TEST_CONNSTR");
            if (!string.IsNullOrEmpty(envVar))
            {
                return envVar;
            }

            return defaultConnStr;
        }
    }

    const string defaultConnStr = @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;";
}