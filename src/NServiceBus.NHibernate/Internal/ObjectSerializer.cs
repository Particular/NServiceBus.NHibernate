namespace NServiceBus.Persistence.NHibernate
{
    using Newtonsoft.Json;

    static class ObjectSerializer
    {
        public static string Serialize<T>(T instance)
        {
            return JsonConvert.SerializeObject(instance);
        }

        public static T DeSerialize<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data);
        }
    }
}